namespace IvanovItog.Shared;

public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public string? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, string? error, T? value) : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, null, value);

    public new static Result<T> Failure(string error) => new(false, error, default);
}
