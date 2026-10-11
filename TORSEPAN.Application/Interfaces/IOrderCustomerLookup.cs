namespace TORSEPAN.Application.Interfaces;

public interface IOrderCustomerLookup
{
    Task<string?> GetCustomerNameAsync(Guid topBowlId, Guid? handpanId, CancellationToken ct);
}
