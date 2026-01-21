using System;

namespace ERFX_Q03UDV_20260121_01
{
    public interface IMessageSubscriber : IDisposable
    {
        event Action<string, string> MessageReceived;
        bool IsConnected { get; }
        void Connect();
        void Disconnect();
        void Subscribe(string topicPattern);
    }
}
