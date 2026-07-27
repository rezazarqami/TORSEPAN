namespace TORSEPAN.Panel.Configuration;

public sealed class ApiConfiguration
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = "https://localhost:5001/";
}