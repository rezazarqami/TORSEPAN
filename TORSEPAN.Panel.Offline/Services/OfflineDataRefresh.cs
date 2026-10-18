namespace TORSEPAN.Panel.Services;
public sealed class OfflineDataRefresh:ILocalDataRefresh{public event Action? DataChanged;public void Notify()=>DataChanged?.Invoke();}
