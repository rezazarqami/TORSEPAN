using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Infrastructure.Services;

public sealed class ProductionCodeGenerator : ICodeGenerator
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionCodeGenerator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateProductionCodeAsync(
        CancellationToken cancellationToken = default)
    {
        var lastCode = await _unitOfWork.Bowls.GetLastProductionCodeAsync();

        if (string.IsNullOrWhiteSpace(lastCode))
        {
            return "0001";
        }

        if (!int.TryParse(lastCode, out var lastNumber))
        {
            throw new InvalidOperationException(
                $"Production code '{lastCode}' is not valid.");
        }

        lastNumber++;

        return lastNumber.ToString("D4");
    }
}