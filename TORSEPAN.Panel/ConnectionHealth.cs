using Microsoft.JSInterop;

namespace TORSEPAN.Panel;

public static class ConnectionHealth
{
    [JSInvokable("ConnectionProbe")]
    public static long Probe() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
