namespace DrawTogether.Shared.Messages;

// Simple JSON envelope for TCP messages.
// Example:
// { "type": "auth.signin", "token": null, "payloadJson": "{\"username\":\"huutien\",\"password\":\"123456\"}" }
public sealed class NetworkRequest
{
    public string Type { get; init; } = string.Empty;
    public string? Token { get; init; }
    public string PayloadJson { get; init; } = "{}";
}

public sealed class NetworkResponse
{
    public string Type { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = "{}";

    public static NetworkResponse Ok(string type, string payloadJson = "{}", string message = "OK")
        => new() { Type = type, Success = true, Message = message, PayloadJson = payloadJson };

    public static NetworkResponse Fail(string type, string message)
        => new() { Type = type, Success = false, Message = message, PayloadJson = "{}" };
}
