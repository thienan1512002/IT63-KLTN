namespace Web.Common;

public class DataResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public static DataResult Success(string message = null)
        => new() { IsSuccess = true, Message = message };

    public static DataResult Failure(string message)
        => new() { IsSuccess = false, Message = message };

    public static DataResult Failure(IEnumerable<string> errors)
        => new() { IsSuccess = false, Errors = errors.ToList() };
}

public class DataResult<T> : DataResult
{
    public T Data { get; set; }

    public static DataResult<T> Success(T data, string message = null)
        => new() { IsSuccess = true, Data = data, Message = message };

    public new static DataResult<T> Failure(string message)
        => new() { IsSuccess = false, Message = message };

    public new static DataResult<T> Failure(IEnumerable<string> errors)
        => new() { IsSuccess = false, Errors = errors.ToList() };
}
