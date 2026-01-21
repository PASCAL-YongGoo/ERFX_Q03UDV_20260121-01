using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace ERFX_Q03UDV_20260121_01
{
    public class ZeroMqSubscriber : IMessageSubscriber
    {
        private readonly string _endpoint;
        private SubscriberSocket _socket;
        private Thread _receiveThread;
        private volatile bool _running;
        private bool _disposed;

        public event Action<string, string> MessageReceived;
        public bool IsConnected { get; private set; }

        public ZeroMqSubscriber(string endpoint)
        {
            _endpoint = endpoint;
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            try
            {
                _socket = new SubscriberSocket();
                _socket.Connect(_endpoint);
                IsConnected = true;

                _running = true;
                _receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "ZeroMQ Subscriber"
                };
                _receiveThread.Start();
            }
            catch (Exception)
            {
                _socket?.Dispose();
                _socket = null;
                IsConnected = false;
                throw;
            }
        }

        public Task ConnectAsync()
        {
            Connect();
            return Task.CompletedTask;
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _running = false;

            try
            {
                _socket?.Disconnect(_endpoint);
                _socket?.Close();
            }
            finally
            {
                _socket?.Dispose();
                _socket = null;
                IsConnected = false;
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(1000);
            }
        }

        public void Subscribe(string topicPattern)
        {
            if (_socket == null)
                return;

            _socket.Subscribe(topicPattern);
        }

        public Task SubscribeAsync(string topicPattern)
        {
            Subscribe(topicPattern);
            return Task.CompletedTask;
        }

        private void ReceiveLoop()
        {
            while (_running && _socket != null)
            {
                try
                {
                    if (_socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out string topic))
                    {
                        if (_socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out string message))
                        {
                            MessageReceived?.Invoke(topic, message);
                        }
                    }
                }
                catch (Exception)
                {
                    if (!_running)
                        break;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Disconnect();
            }

            _disposed = true;
        }
    }
}
