using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DrawTogether.Client.Network
{
public class ReceiveThread
{
public Action<string> OnMessageReceived;


    private readonly Stream _stream;

    private Thread _thread;

    private bool _running;

    public ReceiveThread(Stream stream)
    {
        _stream = stream;
    }

    public void Start()
    {
        _running = true;

        _thread = new Thread(ReceiveLoop);

        _thread.IsBackground = true;

        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[4096];

        while (_running)
        {
            try
            {
                int bytesRead =
                    _stream.Read(
                        buffer,
                        0,
                        buffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine(
                        "Disconnected from server");

                    break;
                }

                string message =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead);

                OnMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Receive error: "
                    + ex.Message);

                break;
            }
        }
    }
}

}
