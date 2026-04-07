using EventBus;
using InventoryService;
using NotificationProcessor;
using OrderService.Services;
using PaymentService;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Event-Driven Order Processing System",
        Version = "v1",
        Description = "Microservices order processing with event-driven architecture — Order, Payment, Inventory, Notification services communicating via events."
    });
});

// Core event bus — singleton so all services share same bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Order service
builder.Services.AddSingleton<IOrderService, OrderServiceImpl>();

// Background services — each listens to events
builder.Services.AddHostedService<PaymentProcessor>();
builder.Services.AddHostedService<InventoryProcessor>();
builder.Services.AddHostedService<OrderNotificationProcessor>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Processing API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "OrderProcessingSystem", Timestamp = DateTime.UtcNow }));

Log.Information("Order Processing System started — Event-Driven Architecture with 4 services");
app.Run();
