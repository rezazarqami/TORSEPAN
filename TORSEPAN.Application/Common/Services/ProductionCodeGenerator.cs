using System.Text.RegularExpressions;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Common.Services;

public class ProductionCodeGenerator : ICodeGenerator
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionCodeGenerator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateBowlCodeAsync()
    {
        var lastCode = await _unitOfWork.Bowls.GetLastProductionCodeAsync();

        if (string.IsNullOrWhiteSpace(lastCode))
            return "B000001";

        var number = int.Parse(Regex.Match(lastCode, @"\d+").Value);

        return $"B{number + 1:D6}";
    }

    public Task<string> GenerateHandpanSerialAsync()
    {
        return Task.FromResult($"HP-{Guid.NewGuid():N[..8]}");
    }

    public Task<string> GenerateWarrantyNumberAsync()
    {
        return Task.FromResult($"WR-{Guid.NewGuid():N[..8]}");
    }

    public Task<string> GeneratePassportNumberAsync()
    {
        return Task.FromResult($"PP-{Guid.NewGuid():N[..8]}");
    }
}