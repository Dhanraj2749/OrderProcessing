using System.ComponentModel.DataAnnotations;
using EventBus.Models;

namespace OrderService.Models;

public enum OrderStatus
{
    Pending,
    PaymentProcessing,
    InventoryReserving,
    Confirmed,
    Shipped,
    Cancelled
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CancellationReason { get; set; }
}

public class CreateOrderRequest
{
    [Required] public string CustomerId { get; set; } = string.Empty;
    [Required][MinLength(1)] public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    [Required] public string ProductId { get; set; } = string.Empty;
    [Required] public string ProductName { get; set; } = string.Empty;
    [Range(1, 100)] public int Quantity { get; set; }
    [Range(0.01, double.MaxValue)] public decimal UnitPrice { get; set; }
}
