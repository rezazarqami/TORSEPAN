using System.Diagnostics.CodeAnalysis;

namespace TORSEPAN.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new ArgumentException(
                "A successful result cannot contain an error.",
                nameof(error));

        if (!isSuccess && error == Error.None)
            throw new ArgumentException(
                "A failed result must contain an error.",
                nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success()
        => new(true, Error.None);

    public static Result Failure(Error error)
        => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(
        T? value,
        bool isSuccess,
        Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                "Cannot access the value of a failed result.");

    public static Result<T> Success([DisallowNull] T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(
            value,
            true,
            Error.None);
    }

    public new static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(
            default,
            false,
            error);
    }
}