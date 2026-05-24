// Chuyen Message <-> JSON string/byte de gui qua socket.
// Neu co ma hoa, serializer se lam viec chung voi CryptoHelper.
using System.Text.Json;

namespace DrawTogether.Shared.Messages
{
    public static class MessageSerializer
    {
        public static string Serialize(Message msg)
        {
            return JsonSerializer.Serialize(msg);
        }

        public static Message Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }
    }
}