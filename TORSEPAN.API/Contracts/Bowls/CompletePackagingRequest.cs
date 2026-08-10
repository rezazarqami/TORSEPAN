namespace TORSEPAN.API.Contracts.Bowls;

public sealed record CompletePackagingRequest(IReadOnlyCollection<Guid>? MaterialIds);
