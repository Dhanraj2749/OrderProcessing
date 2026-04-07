using System.Collections.Concurrent;
using EventBus;
using EventBus.Models;
using OrderService.Models;

namespace OrderService.Services;

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(CreateOrderRequest request);
    Task<Order?> GetOrderAsync(Guid id);
    Task<IEnumerable<Order>> GetOrdersAsync(string? customerId = null);
    Task<bool> CancelOrderAsync(Guid id, string reason);
    Task<object> GetStatsAsync();
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? trackingNumber = null);
}

public class OrderServiceImpl : IOrderService
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderServiceImpl> _logger;

    public OrderServiceImpl(IEventBus eventBus, ILogger<OrderServiceImpl> logger)
    {
        _eventBus = eventBus;
        _logger = logger;

        // Subscribe to events from other services
        _eventBus.Subscribe<PaymentProcessedEvent>(OnPaymentProcessed);
        _eventBus.Subscribe<InventoryReservedEvent>(OnInventoryReserved);
        _eventBus.Subscribe<OrderShippedEvent>(OnOrderShipped);
    }

    public async Task<Order> PlaceOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            Status = OrderStatus.PaymentProcessing
        };

        _orders[order.Id] = order;

        // Publish OrderPlaced event — triggers payment and inventory services
        await _eventBus.PublishAsync(new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items,
            TotalAmount = order.TotalAmount
        });

        _logger.LogInformation("Order {OrderId} placed — total: ${Total}", order.Id, order.TotalAmount);
        return order;
    }

    public Task<Order?> GetOrderAsync(Guid id)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IEnumerable<Order>> GetOrdersAsync(string? customerId = null)
    {
        var orders = _orders.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(customerId))
            orders = orders.Where(o => o.CustomerId == customerId);
        return Task.FromResult<IEnumerable<Order>>(orders.OrderByDescending(o => o.CreatedAt).ToList());
    }

    public async Task<bool> CancelOrderAsync(Guid id, string reason)
    {
        if (!_orders.TryGetValue(id, out var order)) return false;
        if (order.Status == OrderStatus.Shipped) return false;

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        await _eventBus.PublishAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Reason = reason
        });

        return true;
    }

    public Task<object> GetStatsAsync()
    {
        var orders = _orders.Values.ToList();
        return Task.FromResult<object>(new
        {
            Total = orders.Count,
            Pending = orders.Count(o => o.Status == OrderStatus.Pending),
            PaymentProcessing = orders.Count(o => o.Status == OrderStatus.PaymentProcessing),
            Confirmed = orders.Count(o => o.Status == OrderStatus.Confirmed),
            Shipped = orders.Count(o => o.Status == OrderStatus.Shipped),
            Cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled),
            TotalRevenue = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount)
        });
    }

    public Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? trackingNumber = null)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            if (trackingNumber != null) order.TrackingNumber = trackingNumber;
        }
        return Task.CompletedTask;
    }

    private async Task OnPaymentProcessed(PaymentProcessedEvent e)
    {
        if (!_orders.TryGetValue(e.OrderId, out var order)) return;

        if (e.Success)
        {
            order.Status = OrderStatus.InventoryReserving;
            _logger.LogInformation("Order {OrderId} payment confirmed — awaiting inventory", e.OrderId);
        }
        else
        {
            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = "Payment failed";
            _logger.LogWarning("Order {OrderId} cancelled — payment failed", e.OrderId);
        }
        order.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    private async Task OnInventoryReserved(InventoryReservedEvent e)
    {
        if (!_orders.TryGetValue(e.OrderId, out var order)) return;
        order.Status = e.Success ? OrderStatus.Confirmed : OrderStatus.Cancelled;
        if (!e.Success) order.CancellationReason = e.FailureReason;
        order.UpdatedAt = DateTime.UtcNow;
        _logger.LogInformation("Order {OrderId} inventory {Result}", e.OrderId, e.Success ? "reserved" : "failed");
        await Task.CompletedTask;
    }

    private async Task OnOrderShipped(OrderShippedEvent e)
    {
        if (!_orders.TryGetValue(e.OrderId, out var order)) return;
        order.Status = OrderStatus.Shipped;
        order.TrackingNumber = e.TrackingNumber;
        order.UpdatedAt = DateTime.UtcNow;
        _logger.LogInformation("Order {OrderId} shipped — tracking: {Tracking}", e.OrderId, e.TrackingNumber);
        await Task.CompletedTask;
    }
}
