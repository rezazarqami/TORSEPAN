namespace TORSEPAN.Domain.Enums;

[Flags]
public enum ScaleUsage
{
    None = 0,
    TopBowl = 1,
    BottomBowl = 2,
    Handpan = 4,
    CustomTopBowl = 8,
    CustomBottomBowl = 16,
    CustomHandpan = 32,
    Custom = CustomTopBowl | CustomBottomBowl | CustomHandpan,
    All = TopBowl | BottomBowl | Handpan | Custom
}
