using System;
using System.IO;
using System.Threading;
using DrawTogether.Shared.Messages;
using NetworkMessage = DrawTogether.Shared.Messages.Message;

namespace DrawTogether.Client.Network
{
    public sealed class ReceiveThread
    {
        private readonly Stream _stream;
        private readonly CancellationTokenSource _cts = new();
        private Thread? _thread;

        public ReceiveThread(Stream stream)
        {
            _stream = stream;
        }

        public event EventHandler<NetworkMessage>? MessageReceived;
        public event EventHandler<Exception>? ReceiveFailed;

        public void Start()
        {
            _thread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "DrawTogether.Client.ReceiveThread"
            };

            _thread.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private void ReceiveLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var message = MessageSerializer.ReadAsync(_stream, _cts.Token).GetAwaiter().GetResult();
                    MessageReceived?.Invoke(this, message);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (EndOfStreamException ex)
                {
                    ReceiveFailed?.Invoke(this, ex);
                    break;
                }
                catch (Exception ex)
                {
                    ReceiveFailed?.Invoke(this, ex);
                    break;
                }
            }
        }
    }
}
