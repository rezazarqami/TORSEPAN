using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Handpans.Queries.GetHandpanBySerialNumber;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/guarantee-products")]
[AllowAnonymous]
public sealed class GuaranteeProductsController(IMediator mediator, IConfiguration configuration)
    : ControllerBase
{
    [HttpGet("{serialNumber}")]
    public async Task<ActionResult<GuaranteeProductResponse>> Get(
        string serialNumber,
        CancellationToken cancellationToken)
    {
        if (!HasValidApiKey()) return Unauthorized();

        var normalizedSerial = serialNumber.Trim().ToUpperInvariant();
        var handpan = await mediator.Send(
            new GetHandpanBySerialNumberQuery(normalizedSerial), cancellationToken);

        if (handpan is null || handpan.Stage is not ("FinishedWarehouse" or "Sold"))
            return NotFound();

        return Ok(new GuaranteeProductResponse(
            handpan.Id,
            handpan.SerialNumber,
            handpan.Stage,
            handpan.CreatedAt));
    }

    private bool HasValidApiKey()
    {
        var expected = configuration["GuaranteeIntegration:ApiKey"];
        var supplied = Request.Headers["X-Guarantee-Key"].ToString();
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied)) return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

public sealed record GuaranteeProductResponse(
    Guid Id,
    string SerialNumber,
    string Stage,
    DateTime CreatedAtUtc);
