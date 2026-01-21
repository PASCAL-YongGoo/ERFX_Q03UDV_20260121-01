using System;
using System.Threading.Tasks;

namespace ERFX_Q03UDV_20260121_01
{
    public interface IMessageSubscriber : IDisposable
    {
        event Action<string, string> MessageReceived;
        bool IsConnected { get; }
        void Connect();
        Task ConnectAsync();
        void Disconnect();
        void Subscribe(string topicPattern);
        Task SubscribeAsync(string topicPattern);
    }
}
