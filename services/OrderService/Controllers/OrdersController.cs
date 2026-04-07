using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>Place a new order</summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var order = await _orderService.PlaceOrderAsync(request);
        _logger.LogInformation("Order {OrderId} placed for customer {CustomerId}", order.Id, order.CustomerId);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>Get order by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Get all orders</summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? customerId = null)
    {
        var orders = await _orderService.GetOrdersAsync(customerId);
        return Ok(orders);
    }

    /// <summary>Cancel an order</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromQuery] string reason = "Customer request")
    {
        var success = await _orderService.CancelOrderAsync(id, reason);
        return success ? Ok(new { Message = "Order cancelled" }) : NotFound();
    }

    /// <summary>Get order statistics</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _orderService.GetStatsAsync();
        return Ok(stats);
    }
}
