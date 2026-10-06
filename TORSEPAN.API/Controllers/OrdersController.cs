using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TORSEPAN.Application.Orders;
using TORSEPAN.Infrastructure.Services;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/orders"), Authorize]
public sealed class OrdersController(CustomerOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Get(CancellationToken ct) => Ok(await orders.GetAsync(ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await orders.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try { return Ok(await orders.CreateAsync(request, userId, ct)); }
        catch (OrderValidationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("drafts")]
    public Task<IActionResult> Draft(SaveOrderDraftRequest request, CancellationToken ct) => SaveDraft(null, request, ct);
    [HttpPut("{id:guid}/draft")]
    public Task<IActionResult> UpdateDraft(Guid id, SaveOrderDraftRequest request, CancellationToken ct) => SaveDraft(id, request, ct);
    private Task<IActionResult> SaveDraft(Guid? id, SaveOrderDraftRequest request, CancellationToken ct) => Write(async userId =>
        Ok(await orders.SaveDraftAsync(id, request, userId, ct)));
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, SaveOrderDraftRequest request, CancellationToken ct) => Write(async _ =>
        await orders.UpdateOrderAsync(id,request,ct) ? NoContent() : NotFound());
    [HttpPost("{id:guid}/finalize")]
    public Task<IActionResult> Finalize(Guid id, FinalizeOrderRequest request, CancellationToken ct) => Write(async _ =>
        await orders.FinalizeAsync(id, request.Version, ct) ? NoContent() : NotFound());
    [HttpPut("{id:guid}/lines/{lineId:guid}/codes/{slot:int}")]
    public Task<IActionResult> AssignLineCode(Guid id, Guid lineId, int slot, AssignOrderCodeRequest request, CancellationToken ct) => Write(async userId =>
        await orders.AssignLineCodeAsync(id, lineId, slot, request.Code, userId, ct, request.ExpectedCode ?? "") ? NoContent() : NotFound());
    [HttpPut("{id:guid}/code")]
    public Task<IActionResult> AssignCode(Guid id, AssignOrderCodeRequest request, CancellationToken ct) => Write(async userId =>
        await orders.AssignCodeAsync(id, request.Code, userId, ct) ? NoContent() : NotFound());
    private async Task<IActionResult> Write(Func<Guid, Task<IActionResult>> action)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try { return await action(userId); }
        catch (OrderValidationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "سفارش تغییر کرده است؛ فهرست را به‌روز کرده و دوباره تلاش کنید." }); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict(new { message = "این ساز یا جایگاه آن قبلاً ثبت شده است؛ فهرست را به‌روز کنید." }); }
    }
}
