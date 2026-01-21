using System;

namespace ERFX_Q03UDV_20260121_01
{
    public interface IMessagePublisher : IDisposable
    {
        bool IsConnected { get; }
        void Connect();
        void Disconnect();
        void Publish(string topic, string message);
    }
}
