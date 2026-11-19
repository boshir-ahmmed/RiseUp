namespace RiseUp.Framework.Results;

/// <summary>Typed outcome of a use case. Expected business failures are values, not exceptions.</summary>
public class Result
{
    protected Result(bool isSuccess, Error error, string? message)
    {
        IsSuccess = isSuccess;
        Error = error;
        Message = message;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    /// <summary>Optional user-facing success message.</summary>
    public string? Message { get; }

    public static Result Success(string? message = null) => new(true, Error.None, message);
    public static Result Failure(string code, string message) => new(false, new Error(code, message), null);
    public static Result Failure(Error error) => new(false, error, null);
    public static Result<T> Success<T>(T value, string? message = null) => new(value, true, Error.None, message);
    public static Result<T> Failure<T>(string code, string message) => new(default, false, new Error(code, message), null);
    public static Result<T> Failure<T>(Error error) => new(default, false, error, null);
}

public sealed class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, Error error, string? message) : base(isSuccess, error, message) => Value = value;

    public T? Value { get; }

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
