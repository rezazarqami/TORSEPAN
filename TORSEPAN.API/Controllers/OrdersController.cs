using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TORSEPAN.Application.Orders;
using TORSEPAN.Infrastructure.Services;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/orders"), Authorize(Roles = OrderAccess.Roles)]
public sealed class OrdersController(CustomerOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Get(CancellationToken ct) => Ok(await orders.GetAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try { return Ok(await orders.CreateAsync(request, userId, ct)); }
        catch (OrderValidationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:guid}/code")]
    public async Task<IActionResult> AssignCode(Guid id, AssignOrderCodeRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try { return await orders.AssignCodeAsync(id, request.Code, userId, ct) ? NoContent() : NotFound(); }
        catch (OrderValidationException ex) { return BadRequest(ex.Message); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("این ساز به سفارش دیگری متصل شده است؛ فهرست را به‌روز کنید."); }
    }
}
