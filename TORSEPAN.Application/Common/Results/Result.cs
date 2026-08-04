using System.Diagnostics.CodeAnalysis;

namespace TORSEPAN.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
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

    public T? Value => _value;

    public static Result<T> Success([DisallowNull] T value)
        => new(value, true, Error.None);

    public new static Result<T> Failure(Error error)
        => new(default, false, error);
}