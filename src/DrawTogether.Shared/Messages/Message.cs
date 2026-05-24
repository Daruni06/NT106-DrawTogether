// Message chung gui qua TCP socket.
// Nen co type, token, roomId, payload va timestamp.
namespace DrawTogether.Shared.Messages
{
    public class Message
    {
        public MessageType Type { get; set; }

        public string Payload { get; set; }
    }
}