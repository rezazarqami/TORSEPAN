namespace TORSEPAN.Application.Materials;

// Side effects are delayed until the outer offline transaction has committed.
public sealed class InventoryAlertBatch : IDisposable
{
    private static readonly AsyncLocal<InventoryAlertBatch?> Current = new();
    private readonly InventoryAlertBatch? previous;
    private readonly List<Func<Task>> pending = [];
    public InventoryAlertBatch() { previous = Current.Value; Current.Value = this; }
    public static bool TryQueue(Func<Task> alert)
    {
        if (Current.Value is not { } batch) return false;
        batch.pending.Add(alert); return true;
    }
    public async Task FlushAsync() { Dispose(); foreach (var alert in pending) await alert(); pending.Clear(); }
    public void Dispose() { if (ReferenceEquals(Current.Value, this)) Current.Value = previous; }
}
