// Xu ly chat trong phong.
// Nhan tin nhan, validate noi dung, broadcast va co the luu lich su chat.
using System;
using DrawTogether.Shared.Messages;

namespace DrawTogether.Server.Features
{
    public static class ChatService
    {
        public static void Handle(Message msg)
        {
            Console.WriteLine("Chat: " + msg.Payload);
        }
    }
}