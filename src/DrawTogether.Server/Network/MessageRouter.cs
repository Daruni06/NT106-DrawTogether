using System.Text.Json;
using DrawTogether.Server.Features;
using DrawTogether.Shared.Messages;

namespace DrawTogether.Server.Network;

public sealed class MessageRouter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

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

    public async Task<NetworkResponse> RouteAsync(NetworkRequest request, CancellationToken ct = default)
    {
        try
        {
            return request.Type switch
            {
                "auth.signup" =>
                    ToResponse(request.Type,
                        await _auth.SignupAsync(Deserialize<SignupRequest>(request.PayloadJson), ct)),

                "auth.signin" =>
                    ToResponse(request.Type,
                        await _auth.SigninAsync(Deserialize<SigninRequest>(request.PayloadJson), ct)),

                "room.create" =>
                    await Auth(request, async user =>
                        await _rooms.CreateRoomAsync(user, Deserialize<CreateRoomRequest>(request.PayloadJson), ct)),

                "room.join" =>
                    await Auth(request, async user =>
                        await _rooms.JoinRoomAsync(user, Deserialize<JoinRoomRequest>(request.PayloadJson), ct)),

                "draw.history" =>
                    await Auth(request, async user =>
                    {
                        var p = Deserialize<DrawHistoryRequest>(request.PayloadJson);
                        return await _draw.GetCanvasHistoryAsync(user, p.RoomId, p.AfterId, ct);
                    }),

                "draw.save" =>
                    await Auth(request, async user =>
                        await _draw.SaveDrawActionAsync(user,
                            Deserialize<SaveDrawActionRequest>(request.PayloadJson), ct)),

                "chat.send" =>
                    await Auth(request, async user =>
                        await _chat.SendMessageAsync(user,
                            Deserialize<SendChatMessageRequest>(request.PayloadJson), ct)),

                "chat.history" =>
                    await Auth(request, async user =>
                        await _chat.GetHistoryAsync(user,
                            Deserialize<SendChatMessageRequest>(request.PayloadJson).RoomId, ct)),

                _ => NetworkResponse.Fail(request.Type, "Unknown request")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return NetworkResponse.Fail(request.Type, "Server error");
        }
    }

    // =========================
    // AUTH WRAPPER
    // =========================
    private async Task<NetworkResponse> Auth<T>(
        NetworkRequest request,
        Func<AuthenticatedUser, Task<ServiceResult<T>>> handler)
    {
        var auth = _auth.ValidateAccessToken(request.Token ?? "");

        if (!auth.Success || auth.Data is null)
            return NetworkResponse.Fail(request.Type, auth.Message);

        var result = await handler(auth.Data);

        return ToResponse(request.Type, result);
    }

    // =========================
    private static NetworkResponse ToResponse<T>(string type, ServiceResult<T> result)
    {
        if (!result.Success)
            return NetworkResponse.Fail(type, result.Message);

        var json = JsonSerializer.Serialize(result.Data, Options);
        return NetworkResponse.Ok(type, json, result.Message);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options)
           ?? throw new JsonException("Invalid payload");
}