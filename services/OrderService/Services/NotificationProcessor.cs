using EventBus;
using EventBus.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationProcessor;

/// <summary>
/// Listens to all order events and sends notifications to customers.
/// Production: integrate SendGrid / Azure Communication Services.
/// </summary>
public class OrderNotificationProcessor : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderNotificationProcessor> _logger;

    public OrderNotificationProcessor(IEventBus eventBus, ILogger<OrderNotificationProcessor> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<OrderPlacedEvent>(OnOrderPlaced);
        _eventBus.Subscribe<PaymentProcessedEvent>(OnPaymentProcessed);
        _eventBus.Subscribe<OrderShippedEvent>(OnOrderShipped);
        _eventBus.Subscribe<OrderCancelledEvent>(OnOrderCancelled);
        _logger.LogInformation("[NotificationService] Started — subscribed to all order events");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task OnOrderPlaced(OrderPlacedEvent e)
    {
        await Task.Delay(50);
        _logger.LogInformation("[NotificationService] EMAIL → Customer {CustomerId}: 'Your order #{OrderId} has been received. Total: ${Total}'",
            e.CustomerId, e.OrderId.ToString()[..8], e.TotalAmount);
    }

    private async Task OnPaymentProcessed(PaymentProcessedEvent e)
    {
        await Task.Delay(50);
        if (e.Success)
            _logger.LogInformation("[NotificationService] EMAIL → Customer {CustomerId}: 'Payment confirmed for order #{OrderId}'",
                e.CustomerId, e.OrderId.ToString()[..8]);
        else
            _logger.LogInformation("[NotificationService] EMAIL → Customer {CustomerId}: 'Payment failed for order #{OrderId}. Please retry.'",
                e.CustomerId, e.OrderId.ToString()[..8]);
    }

    private async Task OnOrderShipped(OrderShippedEvent e)
    {
        await Task.Delay(50);
        _logger.LogInformation("[NotificationService] SMS → Customer {CustomerId}: 'Order #{OrderId} shipped! Tracking: {Tracking}. Estimated delivery: {Date}'",
            e.CustomerId, e.OrderId.ToString()[..8], e.TrackingNumber, e.EstimatedDelivery.ToShortDateString());
    }

    private async Task OnOrderCancelled(OrderCancelledEvent e)
    {
        await Task.Delay(50);
        _logger.LogInformation("[NotificationService] EMAIL → Customer {CustomerId}: 'Order #{OrderId} cancelled. Reason: {Reason}'",
            e.CustomerId, e.OrderId.ToString()[..8], e.Reason);
    }
}
