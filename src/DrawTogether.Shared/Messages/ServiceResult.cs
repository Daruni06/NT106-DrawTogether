namespace DrawTogether.Shared.Messages;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool success, string message, T? data)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public bool Success { get; }
    public string Message { get; }
    public T? Data { get; }

    public static ServiceResult<T> Ok(T data, string message = "OK")
        => new(true, message, data);

    public static ServiceResult<T> Fail(string message)
        => new(false, message, default);
}

public sealed class EmptyResult
{
    public static readonly EmptyResult Value = new();
    private EmptyResult() { }
}
