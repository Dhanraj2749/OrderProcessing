namespace EventBus.Models;

public abstract class IntegrationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

public class OrderPlacedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class PaymentProcessedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class InventoryReservedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public List<OrderItem> ReservedItems { get; set; } = new();
    public string? FailureReason { get; set; }
}

public class OrderShippedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime EstimatedDelivery { get; set; }
}

public class OrderCancelledEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
