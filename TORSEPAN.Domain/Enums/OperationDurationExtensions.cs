namespace TORSEPAN.Domain.Enums;

public static class OperationDurationExtensions
{
    public static string DisplayName(this OperationDuration duration)
    {
        return duration switch
        {
            OperationDuration.Minutes5 => "5 دقیقه",
            OperationDuration.Minutes10 => "10 دقیقه",
            OperationDuration.Minutes15 => "15 دقیقه",
            OperationDuration.Minutes20 => "20 دقیقه",
            OperationDuration.Minutes25 => "25 دقیقه",
            OperationDuration.Minutes30 => "30 دقیقه",
            OperationDuration.Minutes35 => "35 دقیقه",
            OperationDuration.Minutes40 => "40 دقیقه",
            OperationDuration.Minutes45 => "45 دقیقه",
            OperationDuration.Minutes50 => "50 دقیقه",
            OperationDuration.Minutes55 => "55 دقیقه",
            OperationDuration.Minutes60 => "60 دقیقه",
            OperationDuration.Over60 => "بیش از 60 دقیقه",

            _ => throw new ArgumentOutOfRangeException(nameof(duration))
        };
    }
}