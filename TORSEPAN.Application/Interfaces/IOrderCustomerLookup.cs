namespace TORSEPAN.Application.Interfaces;

public interface IOrderCustomerLookup
{
    Task<IReadOnlyDictionary<Guid,string?>> GetHandpanCustomerNamesAsync(IReadOnlyCollection<Guid> handpanIds, CancellationToken ct);
    Task<string?> GetCustomerNameAsync(Guid topBowlId, Guid? handpanId, CancellationToken ct);
}
