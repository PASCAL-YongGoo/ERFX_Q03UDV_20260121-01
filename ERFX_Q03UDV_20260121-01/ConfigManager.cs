using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ERFX_Q03UDV_20260121_01
{
    [DataContract]
    public class PlcConfig
    {
        [DataMember(Name = "stationNumber")]
        public int StationNumber { get; set; }
    }

    [DataContract]
    public class MonitoringConfig
    {
        [DataMember(Name = "intervalMs")]
        public int IntervalMs { get; set; }
    }

    [DataContract]
    public class DeviceConfig
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }
    }

    [DataContract]
    public class ZeroMqConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "publishEndpoint")]
        public string PublishEndpoint { get; set; }

        [DataMember(Name = "subscribeEndpoint")]
        public string SubscribeEndpoint { get; set; }

        [DataMember(Name = "subscribeEnabled")]
        public bool SubscribeEnabled { get; set; }

        [DataMember(Name = "topicPrefix")]
        public string TopicPrefix { get; set; }
    }

    [DataContract]
    public class MqttConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "broker")]
        public string Broker { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }

        [DataMember(Name = "topicPrefix")]
        public string TopicPrefix { get; set; }

        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        [DataMember(Name = "subscribeEnabled")]
        public bool SubscribeEnabled { get; set; }
    }

    [DataContract]
    public class BarcodeConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "ipAddress")]
        public string IpAddress { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }

        [DataMember(Name = "triggerBitPosition")]
        public int TriggerBitPosition { get; set; }

        [DataMember(Name = "connectionTimeoutMs")]
        public int ConnectionTimeoutMs { get; set; }

        [DataMember(Name = "autoReconnect")]
        public bool AutoReconnect { get; set; }

        [DataMember(Name = "triggerCommand")]
        public string TriggerCommand { get; set; }
    }

    [DataContract]
    public class AppConfig
    {
        [DataMember(Name = "plc")]
        public PlcConfig Plc { get; set; }

        [DataMember(Name = "monitoring")]
        public MonitoringConfig Monitoring { get; set; }

        [DataMember(Name = "devices")]
        public List<DeviceConfig> Devices { get; set; }

        [DataMember(Name = "zeromq")]
        public ZeroMqConfig ZeroMq { get; set; }

        [DataMember(Name = "mqtt")]
        public MqttConfig Mqtt { get; set; }

        [DataMember(Name = "barcode")]
        public BarcodeConfig Barcode { get; set; }
    }

    public class ConfigManager
    {
        private readonly string _configPath;
        public AppConfig Config { get; private set; }

        public ConfigManager(string configPath)
        {
            _configPath = configPath;
        }

        public bool Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Config = CreateDefaultConfig();
                    Save();
                    return true;
                }

                string json = File.ReadAllText(_configPath, Encoding.UTF8);
                var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    Config = (AppConfig)serializer.ReadObject(ms);
                }
                return true;
            }
            catch (Exception)
            {
                Config = CreateDefaultConfig();
                return false;
            }
        }

        public bool Save()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                using (var ms = new MemoryStream())
                {
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true, "  "))
                    {
                        serializer.WriteObject(writer, Config);
                        writer.Flush();
                    }
                    string json = Encoding.UTF8.GetString(ms.ToArray());
                    File.WriteAllText(_configPath, json, Encoding.UTF8);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                Plc = new PlcConfig { StationNumber = 3 },
                Monitoring = new MonitoringConfig { IntervalMs = 100 },
                Devices = new List<DeviceConfig>
                {
                    new DeviceConfig { Name = "데이터1", Address = "D0", Type = "Word" },
                    new DeviceConfig { Name = "데이터2", Address = "D10", Type = "Word" },
                    new DeviceConfig { Name = "비트1", Address = "M0", Type = "Bit" },
                    new DeviceConfig { Name = "입력1", Address = "X0", Type = "Bit" },
                    new DeviceConfig { Name = "출력1", Address = "Y0", Type = "Bit" }
                },
                ZeroMq = new ZeroMqConfig
                {
                    Enabled = true,
                    PublishEndpoint = "tcp://*:5555",
                    SubscribeEndpoint = "tcp://localhost:5556",
                    SubscribeEnabled = true,
                    TopicPrefix = "plc"
                },
                Mqtt = new MqttConfig
                {
                    Enabled = true,
                    Broker = "localhost",
                    Port = 1883,
                    TopicPrefix = "plc",
                    ClientId = "Q03UDV_Monitor",
                    SubscribeEnabled = true
                },
                Barcode = new BarcodeConfig
                {
                    Enabled = false,
                    IpAddress = "192.168.20.10",
                    Port = 8080,
                    TriggerBitPosition = 0,
                    ConnectionTimeoutMs = 3000,
                    AutoReconnect = true,
                    TriggerCommand = "+"
                }
            };
        }

        public List<DeviceItem> CreateDeviceItems()
        {
            var items = new List<DeviceItem>();
            if (Config?.Devices != null)
            {
                foreach (var device in Config.Devices)
                {
                    items.Add(new DeviceItem
                    {
                        Name = device.Name,
                        Address = device.Address,
                        Type = device.Type,
                        Value = 0
                    });
                }
            }
            return items;
        }
    }
}
