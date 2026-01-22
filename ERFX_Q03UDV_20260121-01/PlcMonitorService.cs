using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ERFX_Q03UDV_20260121_01
{
    /// <summary>
    /// PLC 모니터링 비즈니스 로직을 담당하는 서비스 클래스.
    /// UI와 분리된 순수 로직만 처리합니다.
    /// </summary>
    public class PlcMonitorService : IDisposable
    {
        #region Constants

        private const int MAX_RECONNECT_ATTEMPTS = 3;
        private const int RECONNECT_INTERVAL_MS = 5000;

        #endregion

        #region Fields

        private static readonly DataContractJsonSerializer _publishMessageSerializer =
            new DataContractJsonSerializer(typeof(PublishMessage));
        private static readonly DataContractJsonSerializer _writeCommandSerializer =
            new DataContractJsonSerializer(typeof(WriteCommand));

        private readonly ConfigManager _configManager;
        private readonly PlcManager _plcManager;
        private readonly BindingList<DeviceItem> _deviceItems;

        private IMessagePublisher _zmqPublisher;
        private IMessagePublisher _mqttPublisher;
        private IMessageSubscriber _zmqSubscriber;
        private IMessageSubscriber _mqttSubscriber;

        private BarcodeReaderClient _barcodeClient;
        private int _previousD8008Value = -1; // D8008의 이전 값 저장

        private string _zmqTopicPrefix;
        private string _mqttTopicPrefix;

        private CancellationTokenSource _monitorCts;
        private Task _monitorTask;
        private int _reconnectAttempts;
        private DateTime _lastReconnectAttempt = DateTime.MinValue;
        private bool _disposed;

        #endregion

        #region Events

        /// <summary>
        /// 디바이스 값이 변경되었을 때 발생
        /// </summary>
        public event Action DeviceValuesChanged;

        /// <summary>
        /// PLC 연결 상태가 변경되었을 때 발생
        /// </summary>
        public event Action<bool> PlcConnectionStateChanged;

        /// <summary>
        /// Publisher 연결 상태가 변경되었을 때 발생
        /// </summary>
        public event Action<bool, bool> PublisherStateChanged; // (zmqConnected, mqttConnected)

        /// <summary>
        /// 오류 발생 시
        /// </summary>
        public event Action<string, string> ErrorOccurred; // (title, message)

        /// <summary>
        /// 자동 재연결 실패 시
        /// </summary>
        public event Action AutoReconnectFailed;

        #endregion

        #region Properties

        /// <summary>
        /// PLC 연결 상태
        /// </summary>
        public bool IsPlcConnected => _plcManager?.IsConnected ?? false;

        /// <summary>
        /// ZeroMQ Publisher 연결 상태
        /// </summary>
        public bool IsZmqConnected => _zmqPublisher?.IsConnected ?? false;

        /// <summary>
        /// MQTT Publisher 연결 상태
        /// </summary>
        public bool IsMqttConnected => _mqttPublisher?.IsConnected ?? false;

        /// <summary>
        /// 모니터링 중인 디바이스 목록
        /// </summary>
        public BindingList<DeviceItem> DeviceItems => _deviceItems;

        /// <summary>
        /// 모니터링 주기 (ms)
        /// </summary>
        public int MonitoringIntervalMs => _configManager.Config.Monitoring.IntervalMs;

        /// <summary>
        /// 모니터링 중 여부
        /// </summary>
        public bool IsMonitoring => _monitorTask != null && !_monitorTask.IsCompleted;

        #endregion

        #region Constructor

        public PlcMonitorService(string configPath)
        {
            _configManager = new ConfigManager(configPath);
            if (!_configManager.Load())
            {
                ErrorOccurred?.Invoke("경고", "설정 파일 로드에 실패했습니다. 기본값을 사용합니다.");
            }

            _plcManager = new PlcManager(_configManager.Config.Plc.StationNumber);
            _deviceItems = new BindingList<DeviceItem>(_configManager.CreateDeviceItems());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 서비스를 초기화합니다 (Publisher, Subscriber 연결)
        /// </summary>
        public async Task InitializeAsync()
        {
            await InitializePublishersAsync();
            CacheDeviceTopics();
            await InitializeSubscribersAsync();
            InitializeBarcodeClient();
        }

        /// <summary>
        /// PLC에 연결합니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool Connect()
        {
            int result = _plcManager.Connect();
            if (result == 0)
            {
                PlcConnectionStateChanged?.Invoke(true);
                return true;
            }
            else
            {
                ErrorOccurred?.Invoke("오류", $"PLC 연결 실패\n{PlcManager.GetErrorMessage(result)}");
                return false;
            }
        }

        /// <summary>
        /// PLC 연결을 해제합니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool Disconnect()
        {
            int result = _plcManager.Disconnect();
            if (result != 0)
            {
                ErrorOccurred?.Invoke("오류", $"PLC 연결 해제 실패\n{PlcManager.GetErrorMessage(result)}");
                return false;
            }
            PlcConnectionStateChanged?.Invoke(false);
            return true;
        }

        /// <summary>
        /// 모니터링을 시작합니다.
        /// </summary>
        public void StartMonitoring()
        {
            if (_monitorTask != null && !_monitorTask.IsCompleted)
                return;

            _monitorCts = new CancellationTokenSource();
            _monitorTask = MonitorLoopAsync(_monitorCts.Token);
        }

        /// <summary>
        /// 모니터링을 중지합니다.
        /// </summary>
        public void StopMonitoring()
        {
            _monitorCts?.Cancel();
            _monitorTask = null;
        }

        /// <summary>
        /// PLC 디바이스에 값을 씁니다.
        /// </summary>
        /// <param name="address">디바이스 주소</param>
        /// <param name="value">쓸 값</param>
        /// <returns>성공 여부 및 에러 메시지</returns>
        public (bool success, string errorMessage) WriteDevice(string address, int value)
        {
            if (!_plcManager.IsConnected)
            {
                return (false, "PLC에 연결되지 않았습니다.");
            }

            // Find device in whitelist
            DeviceItem targetDevice = null;
            foreach (var device in _deviceItems)
            {
                if (device.Address == address)
                {
                    targetDevice = device;
                    break;
                }
            }

            if (targetDevice == null)
            {
                return (false, $"'{address}'는 등록되지 않은 주소입니다.");
            }

            // Bit device validation
            if (targetDevice.Type == "Bit" && value != 0 && value != 1)
            {
                return (false, "비트 타입은 0 또는 1만 입력 가능합니다.");
            }

            int result = _plcManager.WriteDevice(address, value);
            if (result == 0)
            {
                targetDevice.Value = value;
                DeviceValuesChanged?.Invoke();
                System.Diagnostics.Debug.WriteLine($"[INFO] Write success: {address} = {value}");
                return (true, null);
            }
            else
            {
                string errorMsg = PlcManager.GetErrorMessage(result);
                System.Diagnostics.Debug.WriteLine($"[ERROR] Write failed: {address} = {value}, error: {errorMsg}");
                return (false, $"값 쓰기 실패\n{errorMsg}");
            }
        }

        /// <summary>
        /// 특정 디바이스 정보를 가져옵니다.
        /// </summary>
        public DeviceItem GetDevice(int index)
        {
            if (index >= 0 && index < _deviceItems.Count)
                return _deviceItems[index];
            return null;
        }

        #endregion

        #region Private Methods - Initialization

        private void CacheDeviceTopics()
        {
            foreach (var device in _deviceItems)
            {
                device.ZmqTopic = $"{_zmqTopicPrefix}/{device.Address}";
                device.MqttTopic = $"{_mqttTopicPrefix}/{device.Address}";
            }
        }

        private async Task InitializePublishersAsync()
        {
            var zmqConfig = _configManager.Config.ZeroMq;
            if (zmqConfig != null && zmqConfig.Enabled)
            {
                try
                {
                    _zmqPublisher = new ZeroMqPublisher(zmqConfig.PublishEndpoint);
                    await _zmqPublisher.ConnectAsync();
                    _zmqTopicPrefix = zmqConfig.TopicPrefix ?? "plc";
                    System.Diagnostics.Debug.WriteLine($"[INFO] ZeroMQ Publisher connected: {zmqConfig.PublishEndpoint}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] ZeroMQ Publisher init failed: {ex}");
                    ErrorOccurred?.Invoke("경고", $"ZeroMQ Publisher 초기화 실패: {ex.Message}");
                    _zmqPublisher = null;
                }
            }

            var mqttConfig = _configManager.Config.Mqtt;
            if (mqttConfig != null && mqttConfig.Enabled)
            {
                try
                {
                    _mqttPublisher = new MqttPublisher(mqttConfig.Broker, mqttConfig.Port, mqttConfig.ClientId);
                    await _mqttPublisher.ConnectAsync();
                    _mqttTopicPrefix = mqttConfig.TopicPrefix ?? "plc";
                    System.Diagnostics.Debug.WriteLine($"[INFO] MQTT Publisher connected: {mqttConfig.Broker}:{mqttConfig.Port}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] MQTT Publisher init failed: {ex}");
                    ErrorOccurred?.Invoke("경고", $"MQTT 초기화 실패: {ex.Message}");
                    _mqttPublisher = null;
                }
            }

            PublisherStateChanged?.Invoke(IsZmqConnected, IsMqttConnected);
        }

        private async Task InitializeSubscribersAsync()
        {
            var zmqConfig = _configManager.Config.ZeroMq;
            if (zmqConfig != null && zmqConfig.Enabled && zmqConfig.SubscribeEnabled)
            {
                try
                {
                    _zmqSubscriber = new ZeroMqSubscriber(zmqConfig.SubscribeEndpoint);
                    _zmqSubscriber.MessageReceived += OnMessageReceived;
                    await _zmqSubscriber.ConnectAsync();
                    await _zmqSubscriber.SubscribeAsync(zmqConfig.TopicPrefix + "/");
                    System.Diagnostics.Debug.WriteLine($"[INFO] ZeroMQ Subscriber connected: {zmqConfig.SubscribeEndpoint}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] ZeroMQ Subscriber init failed: {ex}");
                    ErrorOccurred?.Invoke("경고", $"ZeroMQ Subscriber 초기화 실패: {ex.Message}");
                    _zmqSubscriber = null;
                }
            }

            var mqttConfig = _configManager.Config.Mqtt;
            if (mqttConfig != null && mqttConfig.Enabled && mqttConfig.SubscribeEnabled)
            {
                try
                {
                    _mqttSubscriber = new MqttSubscriber(mqttConfig.Broker, mqttConfig.Port, mqttConfig.ClientId);
                    _mqttSubscriber.MessageReceived += OnMessageReceived;
                    await _mqttSubscriber.ConnectAsync();
                    await _mqttSubscriber.SubscribeAsync($"{mqttConfig.TopicPrefix}/+/set");
                    System.Diagnostics.Debug.WriteLine($"[INFO] MQTT Subscriber connected: {mqttConfig.Broker}:{mqttConfig.Port}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] MQTT Subscriber init failed: {ex}");
                    ErrorOccurred?.Invoke("경고", $"MQTT Subscriber 초기화 실패: {ex.Message}");
                    _mqttSubscriber = null;
                }
            }
        }

        private void InitializeBarcodeClient()
        {
            var barcodeConfig = _configManager.Config.Barcode;
            if (barcodeConfig != null && barcodeConfig.Enabled)
            {
                try
                {
                    _barcodeClient = new BarcodeReaderClient(
                        barcodeConfig.IpAddress,
                        barcodeConfig.Port,
                        barcodeConfig.ConnectionTimeoutMs,
                        barcodeConfig.AutoReconnect
                    );

                    _barcodeClient.ConnectionStateChanged += (sender, isConnected) =>
                    {
                        string status = isConnected ? "연결됨" : "연결 끊김";
                        System.Diagnostics.Debug.WriteLine($"[INFO] Barcode Reader {status}: {barcodeConfig.IpAddress}:{barcodeConfig.Port}");
                    };

                    _barcodeClient.ErrorOccurred += (sender, errorMsg) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Barcode Reader: {errorMsg}");
                    };

                    // 초기 연결 시도
                    _ = _barcodeClient.ConnectAsync();

                    System.Diagnostics.Debug.WriteLine($"[INFO] Barcode Reader Client initialized: {barcodeConfig.IpAddress}:{barcodeConfig.Port}, Trigger Bit: {barcodeConfig.TriggerBitPosition}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Barcode Reader Client init failed: {ex}");
                    ErrorOccurred?.Invoke("경고", $"Barcode Reader 초기화 실패: {ex.Message}");
                    _barcodeClient = null;
                }
            }
        }

        #endregion

        #region Private Methods - Monitoring Loop

        private async Task MonitorLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!_plcManager.IsConnected)
                    {
                        await TryAutoReconnectAsync();
                    }
                    else
                    {
                        ReadAndPublish();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Monitor loop: {ex.Message}");
                }

                try
                {
                    await Task.Delay(MonitoringIntervalMs, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void ReadAndPublish()
        {
            bool hasChanges;
            bool success = _plcManager.ReadDevices(new List<DeviceItem>(_deviceItems), out hasChanges);

            if (!success)
            {
                System.Diagnostics.Debug.WriteLine("[WARN] PLC read failed, will attempt reconnect");
                PlcConnectionStateChanged?.Invoke(false);
                return;
            }

            _reconnectAttempts = 0;

            // D8008 비트 변화 감지 및 바코드 트리거
            CheckD8008AndTriggerBarcode();

            if (hasChanges)
            {
                PublishDeviceValues();
                DeviceValuesChanged?.Invoke();
            }
        }

        private async Task TryAutoReconnectAsync()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Auto-reconnect failed after {MAX_RECONNECT_ATTEMPTS} attempts");
                StopMonitoring();
                AutoReconnectFailed?.Invoke();
                return;
            }

            var now = DateTime.Now;
            if ((now - _lastReconnectAttempt).TotalMilliseconds >= RECONNECT_INTERVAL_MS)
            {
                _lastReconnectAttempt = now;
                _reconnectAttempts++;

                System.Diagnostics.Debug.WriteLine($"[INFO] Auto-reconnect attempt {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}");

                int result = _plcManager.Reconnect();
                if (result == 0)
                {
                    _reconnectAttempts = 0;
                    System.Diagnostics.Debug.WriteLine("[INFO] Auto-reconnect successful");
                    PlcConnectionStateChanged?.Invoke(true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[WARN] Auto-reconnect failed: {PlcManager.GetErrorMessage(result)}");
                    PlcConnectionStateChanged?.Invoke(false);
                }
            }

            await Task.CompletedTask;
        }

        private void CheckD8008AndTriggerBarcode()
        {
            if (_barcodeClient == null || _configManager.Config.Barcode == null || !_configManager.Config.Barcode.Enabled)
                return;

            // D8008 디바이스 찾기
            DeviceItem d8008Device = null;
            foreach (var device in _deviceItems)
            {
                if (device.Address == "D8008")
                {
                    d8008Device = device;
                    break;
                }
            }

            if (d8008Device == null)
                return;

            int currentValue = d8008Device.Value;
            int triggerBitPosition = _configManager.Config.Barcode.TriggerBitPosition;

            // 이전 값이 설정되지 않았으면 현재 값을 저장하고 종료
            if (_previousD8008Value == -1)
            {
                _previousD8008Value = currentValue;
                return;
            }

            // 특정 비트가 0→1로 변경되었는지 확인
            int bitMask = 1 << triggerBitPosition;
            bool previousBit = (_previousD8008Value & bitMask) != 0;
            bool currentBit = (currentValue & bitMask) != 0;

            if (!previousBit && currentBit)
            {
                // 비트가 0에서 1로 변경됨 - 트리거 전송
                string triggerCommand = _configManager.Config.Barcode.TriggerCommand ?? "+";
                System.Diagnostics.Debug.WriteLine($"[INFO] D8008 bit {triggerBitPosition} changed 0→1, sending barcode trigger: '{triggerCommand}'");

                _ = _barcodeClient.SendTriggerAsync(triggerCommand);
            }

            // 현재 값을 이전 값으로 저장
            _previousD8008Value = currentValue;
        }

        #endregion

        #region Private Methods - Messaging

        private void OnMessageReceived(string topic, string message)
        {
            ProcessWriteCommand(topic, message);
        }

        private void ProcessWriteCommand(string topic, string message)
        {
            try
            {
                if (!topic.EndsWith("/set"))
                    return;

                string address = ExtractAddressFromTopic(topic);
                if (string.IsNullOrEmpty(address))
                    return;

                DeviceItem targetDevice = null;
                foreach (var device in _deviceItems)
                {
                    if (device.Address == address)
                    {
                        targetDevice = device;
                        break;
                    }
                }

                if (targetDevice == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARN] Write rejected: address '{address}' not in whitelist");
                    return;
                }

                var command = DeserializeWriteCommand(message);
                if (command == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARN] Invalid write command format for topic '{topic}'");
                    return;
                }

                if (targetDevice.Type == "Bit" && command.Value != 0 && command.Value != 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARN] Write rejected: Bit device '{address}' requires value 0 or 1, got {command.Value}");
                    return;
                }

                if (!_plcManager.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARN] Write failed: PLC not connected (address: {address})");
                    return;
                }

                int valueToWrite = command.Value;
                Task.Run(() => ExecutePlcWriteAsync(address, valueToWrite, targetDevice));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ProcessWriteCommand exception: {ex.Message}");
            }
        }

        private void ExecutePlcWriteAsync(string address, int value, DeviceItem targetDevice)
        {
            try
            {
                int result = _plcManager.WriteDevice(address, value);
                if (result == 0)
                {
                    targetDevice.Value = value;
                    DeviceValuesChanged?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"[INFO] Write success: {address} = {value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Write failed: {address} = {value}, error: {PlcManager.GetErrorMessage(result)}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ExecutePlcWrite exception: {ex.Message}");
            }
        }

        private string ExtractAddressFromTopic(string topic)
        {
            var parts = topic.Split('/');
            if (parts.Length >= 3)
            {
                return parts[parts.Length - 2];
            }
            return null;
        }

        private WriteCommand DeserializeWriteCommand(string json)
        {
            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (WriteCommand)_writeCommandSerializer.ReadObject(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        private void PublishDeviceValues()
        {
            if (_zmqPublisher == null && _mqttPublisher == null)
                return;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            foreach (var device in _deviceItems)
            {
                var message = new PublishMessage
                {
                    Address = device.Address,
                    Name = device.Name,
                    Type = device.Type,
                    Value = device.Value,
                    Timestamp = timestamp
                };

                string json = SerializeToJson(message);

                if (_zmqPublisher != null && _zmqPublisher.IsConnected)
                {
                    _zmqPublisher.Publish(device.ZmqTopic, json);
                }

                if (_mqttPublisher != null && _mqttPublisher.IsConnected)
                {
                    _mqttPublisher.Publish(device.MqttTopic, json);
                }
            }
        }

        private string SerializeToJson(PublishMessage message)
        {
            using (var ms = new MemoryStream())
            {
                _publishMessageSerializer.WriteObject(ms, message);
                return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }

        #endregion

        #region IDisposable

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
                StopMonitoring();
                _monitorCts?.Dispose();
                _plcManager?.Dispose();
                _zmqPublisher?.Dispose();
                _mqttPublisher?.Dispose();
                _zmqSubscriber?.Dispose();
                _mqttSubscriber?.Dispose();
                _barcodeClient?.Dispose();
            }

            _disposed = true;
        }

        ~PlcMonitorService()
        {
            Dispose(false);
        }

        #endregion
    }
}
