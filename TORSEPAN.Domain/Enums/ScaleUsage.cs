namespace TORSEPAN.Domain.Enums;

[Flags]
public enum ScaleUsage
{
    None = 0,
    TopBowl = 1,
    BottomBowl = 2,
    Handpan = 4,
    Custom = 8,
    All = TopBowl | BottomBowl | Handpan | Custom
}
