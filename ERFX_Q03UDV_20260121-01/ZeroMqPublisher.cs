using System;
using NetMQ;
using NetMQ.Sockets;

namespace ERFX_Q03UDV_20260121_01
{
    public class ZeroMqPublisher : IMessagePublisher
    {
        private readonly string _endpoint;
        private PublisherSocket _socket;
        private bool _disposed;

        public bool IsConnected { get; private set; }

        public ZeroMqPublisher(string endpoint)
        {
            _endpoint = endpoint;
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            try
            {
                _socket = new PublisherSocket();
                _socket.Bind(_endpoint);
                IsConnected = true;
            }
            catch (Exception)
            {
                _socket?.Dispose();
                _socket = null;
                IsConnected = false;
                throw;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            try
            {
                _socket?.Unbind(_endpoint);
                _socket?.Close();
            }
            finally
            {
                _socket?.Dispose();
                _socket = null;
                IsConnected = false;
            }
        }

        public void Publish(string topic, string message)
        {
            if (!IsConnected || _socket == null)
                return;

            try
            {
                _socket.SendMoreFrame(topic).SendFrame(message);
            }
            catch (Exception)
            {
                // Ignore publish errors to avoid blocking the main thread
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
