namespace TORSEPAN.API.Models;

public sealed class ErrorResponse
{
    public int StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    public IEnumerable<string>? Errors { get; init; }
}