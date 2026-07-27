namespace TORSEPAN.Application.Common.Results;

public static class ErrorCodes
{
    // General
    public static readonly Error Unknown =
        new("UNKNOWN", "An unknown error has occurred.");

    public static readonly Error Validation =
        new("VALIDATION_ERROR", "One or more validation errors occurred.");

    public static readonly Error NotFound =
        new("NOT_FOUND", "The requested resource was not found.");

    // Bowl
    public static readonly Error BowlNotFound =
        new("BOWL_NOT_FOUND", "The bowl was not found.");

    public static readonly Error InvalidNoteCount =
        new("INVALID_NOTE_COUNT", "The note count is invalid.");

    public static readonly Error InvalidInstrumentType =
        new("INVALID_INSTRUMENT_TYPE", "The instrument type is invalid.");

    public static readonly Error InvalidStage =
        new("INVALID_STAGE", "The production stage is invalid.");

    public static readonly Error TopBowlAlreadyUsed =
        new("TOP_BOWL_ALREADY_USED", "The top bowl has already been used.");

    public static readonly Error BottomBowlAlreadyUsed =
        new("BOTTOM_BOWL_ALREADY_USED", "The bottom bowl has already been used.");
}