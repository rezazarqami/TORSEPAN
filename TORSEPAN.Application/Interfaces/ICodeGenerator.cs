namespace TORSEPAN.Application.Interfaces;

public interface ICodeGenerator
{
    Task<string> GenerateProductionCodeAsync(
        CancellationToken cancellationToken = default);
}