namespace TORSEPAN.Domain.ValueObjects;

public readonly record struct OperationDuration
{
    public int Value { get; }

    private OperationDuration(int value)
    {
        Value = value;
    }

    public static readonly OperationDuration Minutes5 = new(5);
    public static readonly OperationDuration Minutes10 = new(10);
    public static readonly OperationDuration Minutes15 = new(15);
    public static readonly OperationDuration Minutes20 = new(20);
    public static readonly OperationDuration Minutes25 = new(25);
    public static readonly OperationDuration Minutes30 = new(30);
    public static readonly OperationDuration Minutes35 = new(35);
    public static readonly OperationDuration Minutes40 = new(40);
    public static readonly OperationDuration Minutes45 = new(45);
    public static readonly OperationDuration Minutes50 = new(50);
    public static readonly OperationDuration Minutes55 = new(55);
    public static readonly OperationDuration Minutes60 = new(60);

    public static readonly OperationDuration Over60 = new(65);

    public static IReadOnlyList<OperationDuration> All => new[]
    {
        Minutes5,
        Minutes10,
        Minutes15,
        Minutes20,
        Minutes25,
        Minutes30,
        Minutes35,
        Minutes40,
        Minutes45,
        Minutes50,
        Minutes55,
        Minutes60,
        Over60
    };

    public static bool IsValid(int value)
    {
        return All.Any(x => x.Value == value);
    }

    public static OperationDuration Create(int value)
    {
        if (!IsValid(value))
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"'{value}' is not a valid operation duration.");

        return All.First(x => x.Value == value);
    }

    public override string ToString()
    {
        return this == Over60
            ? "بیش از 60 دقیقه"
            : $"{Value} دقیقه";
    }
}