using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.API.Services;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/backups"), Authorize(Roles = "Administrator")]
public sealed class BackupsController(ManualBackupJobs jobs) : ControllerBase
{
    private Guid Owner => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost]
    public IActionResult Create(BackupRequest request)
    {
        if (request.Kind is not ("full" or "pdf")) return BadRequest();
        var job = jobs.Create(Owner, request.Kind);
        return job is null ? StatusCode(429) : Accepted(job);
    }
    [HttpGet("{id:guid}")]
    public IActionResult Status(Guid id) => jobs.Get(id, Owner) is { } job ? Ok(job) : NotFound();
    [HttpPost("{id:guid}/ticket")]
    public IActionResult Ticket(Guid id) => jobs.Ticket(id, Owner) is { } token ? Ok(new { token }) : NotFound();
    // Capability URL expires in five minutes; the authenticated owner must first mint it.
    // This lets the current Android DownloadManager stream files without exposing JWTs in URLs.
    [AllowAnonymous, HttpGet("download/{token}")]
    public IActionResult Download(string token)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (token.Length != 64 || jobs.Download(token) is not { } file) return NotFound();
        return File(file.Stream, file.ContentType, file.FileName, enableRangeProcessing: true);
    }
}
public sealed record BackupRequest(string Kind);
