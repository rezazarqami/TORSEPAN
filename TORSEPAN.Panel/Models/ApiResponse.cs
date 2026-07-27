namespace TORSEPAN.Panel.Models;

public class ApiResponse<T>
{
    public bool Succeeded { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }

    public IEnumerable<string> Errors { get; set; } = [];
}