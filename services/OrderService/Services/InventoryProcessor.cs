using System.Collections.Concurrent;
using EventBus;
using EventBus.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryService;

/// <summary>
/// Listens for PaymentProcessedEvent, reserves inventory,
/// publishes InventoryReservedEvent.
/// If payment succeeded, also triggers shipment.
/// </summary>
public class InventoryProcessor : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<InventoryProcessor> _logger;

    // Simulated inventory store — Production: SQL Server / CosmosDB
    private readonly ConcurrentDictionary<string, int> _inventory = new()
    {
        ["PROD-001"] = 100,
        ["PROD-002"] = 50,
        ["PROD-003"] = 25,
        ["PROD-004"] = 200,
        ["PROD-005"] = 10
    };

    public InventoryProcessor(IEventBus eventBus, ILogger<InventoryProcessor> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<PaymentProcessedEvent>(OnPaymentProcessed);
        _logger.LogInformation("[InventoryService] Started — listening for PaymentProcessedEvent");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[InventoryService] Stopped");
        return Task.CompletedTask;
    }

    private async Task OnPaymentProcessed(PaymentProcessedEvent e)
    {
        if (!e.Success)
        {
            _logger.LogInformation("[InventoryService] Skipping Order {OrderId} — payment failed", e.OrderId);
            return;
        }

        _logger.LogInformation("[InventoryService] Reserving inventory for Order {OrderId}", e.OrderId);
        await Task.Delay(300);

        // Try to reserve all items atomically
        var reservedItems = new List<OrderItem>();
        string? failureReason = null;

        foreach (var item in e.Items ?? new List<OrderItem>())
        {
            var available = _inventory.GetOrAdd(item.ProductId, _ => 50);
            if (available >= item.Quantity)
            {
                _inventory[item.ProductId] = available - item.Quantity;
                reservedItems.Add(item);
                _logger.LogInformation("[InventoryService] Reserved {Qty}x {Product}", item.Quantity, item.ProductId);
            }
            else
            {
                failureReason = $"Insufficient stock for {item.ProductId} (available: {available}, requested: {item.Quantity})";
                break;
            }
        }

        var success = failureReason == null;

        await _eventBus.PublishAsync(new InventoryReservedEvent
        {
            OrderId = e.OrderId,
            Success = success,
            ReservedItems = reservedItems,
            FailureReason = failureReason
        });

        if (success)
        {
            // Trigger shipment
            await Task.Delay(200);
            await _eventBus.PublishAsync(new OrderShippedEvent
            {
                OrderId = e.OrderId,
                CustomerId = e.CustomerId,
                TrackingNumber = $"SHIP-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                EstimatedDelivery = DateTime.UtcNow.AddDays(3)
            });
            _logger.LogInformation("[InventoryService] Order {OrderId} shipped", e.OrderId);
        }
    }
}
