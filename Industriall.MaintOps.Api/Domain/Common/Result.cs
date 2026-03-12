namespace Industriall.MaintOps.Api.Domain.Common;

/// <summary>
/// Represents the outcome of a domain operation that returns a value.
/// </summary>
public sealed class Result<T>
{
    public bool    IsSuccess { get; }
    public bool    IsFailure => !IsSuccess;
    public T?      Value     { get; }
    public string  Error     { get; }

    private Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        Value     = value;
        Error     = error;
    }

    public static Result<T> Success(T value)       => new(true,  value,   string.Empty);
    public static Result<T> Failure(string error)  => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Represents the outcome of a domain operation that does not return a value.
/// </summary>
public sealed class Result
{
    public bool   IsSuccess { get; }
    public bool   IsFailure => !IsSuccess;
    public string Error     { get; }

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error     = error;
    }

    public static Result Success()                => new(true,  string.Empty);
    public static Result Failure(string error)    => new(false, error);
}
