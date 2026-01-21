using System;
using System.Text;
using System.Threading;
using MQTTnet;
using MQTTnet.Client;

namespace ERFX_Q03UDV_20260121_01
{
    public class MqttSubscriber : IMessageSubscriber
    {
        private readonly string _broker;
        private readonly int _port;
        private readonly string _clientId;
        private IMqttClient _client;
        private bool _disposed;

        public event Action<string, string> MessageReceived;
        public bool IsConnected => _client?.IsConnected ?? false;

        public MqttSubscriber(string broker, int port, string clientId)
        {
            _broker = broker;
            _port = port;
            _clientId = clientId + "_sub";
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            try
            {
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                _client.ApplicationMessageReceivedAsync += e =>
                {
                    string topic = e.ApplicationMessage.Topic;
                    var payload = e.ApplicationMessage.PayloadSegment;
                    string message = Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
                    MessageReceived?.Invoke(topic, message);
                    return System.Threading.Tasks.Task.CompletedTask;
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_broker, _port)
                    .WithClientId(_clientId)
                    .WithCleanSession()
                    .Build();

                _client.ConnectAsync(options, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                _client?.Dispose();
                _client = null;
                throw;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            try
            {
                _client?.DisconnectAsync().GetAwaiter().GetResult();
            }
            finally
            {
                _client?.Dispose();
                _client = null;
            }
        }

        public void Subscribe(string topicPattern)
        {
            if (_client == null || !IsConnected)
                return;

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicPattern)
                .Build();

            _client.SubscribeAsync(subscribeOptions, CancellationToken.None).GetAwaiter().GetResult();
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
