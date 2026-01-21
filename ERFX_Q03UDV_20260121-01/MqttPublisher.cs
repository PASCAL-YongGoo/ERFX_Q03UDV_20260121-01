using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace ERFX_Q03UDV_20260121_01
{
    public class MqttPublisher : IMessagePublisher
    {
        private readonly string _broker;
        private readonly int _port;
        private readonly string _clientId;
        private IMqttClient _client;
        private bool _disposed;

        public bool IsConnected => _client?.IsConnected ?? false;

        public MqttPublisher(string broker, int port, string clientId)
        {
            _broker = broker;
            _port = port;
            _clientId = clientId;
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            try
            {
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

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

        public void Publish(string topic, string message)
        {
            if (!IsConnected || _client == null)
                return;

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(message))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            // Fire-and-forget async publish - does not block UI thread
            _ = _client.PublishAsync(mqttMessage, CancellationToken.None);
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
