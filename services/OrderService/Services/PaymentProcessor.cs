using EventBus;
using EventBus.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentService;

/// <summary>
/// Listens for OrderPlacedEvent, simulates payment processing,
/// publishes PaymentProcessedEvent.
/// Production: integrate Stripe / Azure Payment Services.
/// </summary>
public class PaymentProcessor : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<PaymentProcessor> _logger;

    public PaymentProcessor(IEventBus eventBus, ILogger<PaymentProcessor> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<OrderPlacedEvent>(ProcessPaymentAsync);
        _logger.LogInformation("[PaymentService] Started — listening for OrderPlacedEvent");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PaymentService] Stopped");
        return Task.CompletedTask;
    }

    private async Task ProcessPaymentAsync(OrderPlacedEvent e)
    {
        _logger.LogInformation("[PaymentService] Processing payment for Order {OrderId} — Amount: ${Amount}",
            e.OrderId, e.TotalAmount);

        // Simulate payment processing delay
        await Task.Delay(500);

        // Simulate 95% success rate
        var success = Random.Shared.NextDouble() > 0.05;
        var transactionId = success ? $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}" : "";

        await _eventBus.PublishAsync(new PaymentProcessedEvent
        {
            OrderId = e.OrderId,
            CustomerId = e.CustomerId,
            Amount = e.TotalAmount,
            Success = success,
            TransactionId = transactionId,
            Items = e.Items
        });

        _logger.LogInformation("[PaymentService] Payment {Result} for Order {OrderId} | TxnId: {TxnId}",
            success ? "APPROVED" : "DECLINED", e.OrderId, transactionId);
    }
}
