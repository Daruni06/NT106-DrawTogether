// Xu ly su kien ve.
// Kiem tra quyen phong, luu stroke vao database va broadcast cho client khac.
using System;
using DrawTogether.Shared.Messages;

namespace DrawTogether.Server.Features
{
    public static class DrawService
    {
        public static void Handle(Message msg)
        {
            Console.WriteLine("Draw: " + msg.Payload);
        }
    }
}