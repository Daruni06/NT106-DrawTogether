using System.Text.Json;
using DrawTogether.Server.Features;
using DrawTogether.Shared.Messages;

namespace DrawTogether.Server.Network;

// This router is optional but useful for ClientHandler.
// ClientHandler only needs to parse one JSON request, call RouteAsync, then send one JSON response.
public sealed class MessageRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuthService _auth;
    private readonly RoomService _rooms;
    private readonly DrawService _draw;
    private readonly ChatService _chat;

    public MessageRouter(AuthService auth, RoomService rooms, DrawService draw, ChatService chat)
    {
        _auth = auth;
        _rooms = rooms;
        _draw = draw;
        _chat = chat;
    }

    public async Task<NetworkResponse> RouteAsync(
        NetworkRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return request.Type switch
            {
                "auth.signup" => await HandleSignupAsync(request, cancellationToken),
                "auth.signin" => await HandleSigninAsync(request, cancellationToken),
                "auth.signout" => HandleSignout(),

                "room.create" => await HandleAuthorizedAsync(request, async user =>
                    await _rooms.CreateRoomAsync(user, Deserialize<CreateRoomRequest>(request.PayloadJson), cancellationToken)),

                "room.join" => await HandleAuthorizedAsync(request, async user =>
                    await _rooms.JoinRoomAsync(user, Deserialize<JoinRoomRequest>(request.PayloadJson), cancellationToken)),

                "room.leave" => await HandleAuthorizedAsync(request, async user =>
                    await _rooms.LeaveRoomAsync(user, Deserialize<LeaveRoomRequest>(request.PayloadJson), cancellationToken)),

                "room.list" => await ToNetworkResponseAsync(request.Type, await _rooms.ListOpenRoomsAsync(cancellationToken)),

                "draw.save" => await HandleAuthorizedAsync(request, async user =>
                    await _draw.SaveDrawActionAsync(user, Deserialize<SaveDrawActionRequest>(request.PayloadJson), cancellationToken)),

                "draw.history" => await HandleAuthorizedAsync(request, async user =>
                {
                    var payload = Deserialize<DrawHistoryRequest>(request.PayloadJson);
                    return await _draw.GetCanvasHistoryAsync(user, payload.RoomId, payload.AfterId, cancellationToken);
                }),

                "chat.send" => await HandleAuthorizedAsync(request, async user =>
                    await _chat.SendMessageAsync(user, Deserialize<SendChatMessageRequest>(request.PayloadJson), cancellationToken)),

                "chat.history" => await HandleAuthorizedAsync(request, async user =>
                {
                    var payload = Deserialize<ChatHistoryRequest>(request.PayloadJson);
                    return await _chat.GetHistoryAsync(user, payload.RoomId, cancellationToken);
                }),

                _ => NetworkResponse.Fail(request.Type, "Unknown request type.")
            };
        }
        catch (JsonException)
        {
            return NetworkResponse.Fail(request.Type, "Invalid JSON payload.");
        }
        catch (Exception ex)
        {
            // In production, log ex.ToString() on server, but do not leak internal errors to client.
            Console.WriteLine(ex);
            return NetworkResponse.Fail(request.Type, "Internal server error.");
        }
    }

    private async Task<NetworkResponse> HandleSignupAsync(NetworkRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.SignupAsync(Deserialize<SignupRequest>(request.PayloadJson), cancellationToken);
        return ToNetworkResponse(request.Type, result);
    }

    private async Task<NetworkResponse> HandleSigninAsync(NetworkRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.SigninAsync(Deserialize<SigninRequest>(request.PayloadJson), cancellationToken);
        return ToNetworkResponse(request.Type, result);
    }

    private NetworkResponse HandleSignout()
    {
        var result = _auth.Signout();
        return ToNetworkResponse("auth.signout", result);
    }

    private async Task<NetworkResponse> HandleAuthorizedAsync<T>(
        NetworkRequest request,
        Func<AuthenticatedUser, Task<ServiceResult<T>>> handler)
    {
        var authResult = _auth.ValidateAccessToken(request.Token ?? string.Empty);
        if (!authResult.Success || authResult.Data is null)
        {
            return NetworkResponse.Fail(request.Type, authResult.Message);
        }

        var result = await handler(authResult.Data);
        return ToNetworkResponse(request.Type, result);
    }

    private static Task<NetworkResponse> ToNetworkResponseAsync<T>(string type, ServiceResult<T> result)
    {
        return Task.FromResult(ToNetworkResponse(type, result));
    }

    private static NetworkResponse ToNetworkResponse<T>(string type, ServiceResult<T> result)
    {
        if (!result.Success)
        {
            return NetworkResponse.Fail(type, result.Message);
        }

        var payloadJson = JsonSerializer.Serialize(result.Data, JsonOptions);
        return NetworkResponse.Ok(type, payloadJson, result.Message);
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new JsonException($"Cannot deserialize {typeof(T).Name}.");
    }
}

public sealed class DrawHistoryRequest
{
    public string RoomId { get; init; } = string.Empty;
    public long AfterId { get; init; }
}

public sealed class ChatHistoryRequest
{
    public string RoomId { get; init; } = string.Empty;
}
