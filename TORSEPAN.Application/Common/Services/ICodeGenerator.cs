namespace TORSEPAN.Application.Common.Services;

public interface ICodeGenerator
{
    Task<string> GenerateBowlCodeAsync();

    Task<string> GenerateHandpanSerialAsync();

    Task<string> GenerateWarrantyNumberAsync();

    Task<string> GeneratePassportNumberAsync();
}