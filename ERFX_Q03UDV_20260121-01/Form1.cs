using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ERFX_Q03UDV_20260121_01
{
    public partial class Form1 : Form
    {
        private static readonly DataContractJsonSerializer _publishMessageSerializer =
            new DataContractJsonSerializer(typeof(PublishMessage));
        private static readonly DataContractJsonSerializer _writeCommandSerializer =
            new DataContractJsonSerializer(typeof(WriteCommand));

        private ConfigManager _configManager;
        private PlcManager _plcManager;
        private BindingList<DeviceItem> _deviceItems;
        private IMessagePublisher _zmqPublisher;
        private IMessagePublisher _mqttPublisher;
        private IMessageSubscriber _zmqSubscriber;
        private IMessageSubscriber _mqttSubscriber;
        private string _zmqTopicPrefix;
        private string _mqttTopicPrefix;

        // Auto-reconnect settings
        private const int MAX_RECONNECT_ATTEMPTS = 3;
        private const int RECONNECT_INTERVAL_MS = 5000;
        private int _reconnectAttempts;
        private DateTime _lastReconnectAttempt = DateTime.MinValue;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            string configPath = Path.Combine(Application.StartupPath, "config.json");
            _configManager = new ConfigManager(configPath);

            if (!_configManager.Load())
            {
                MessageBox.Show("설정 파일 로드에 실패했습니다. 기본값을 사용합니다.",
                    "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _plcManager = new PlcManager(_configManager.Config.Plc.StationNumber);

            _deviceItems = new BindingList<DeviceItem>(_configManager.CreateDeviceItems());
            dgvDevices.DataSource = _deviceItems;

            tmrMonitor.Interval = _configManager.Config.Monitoring.IntervalMs;
            lblInterval.Text = $"갱신 주기: {tmrMonitor.Interval}ms";

            await InitializePublishersAsync();
            CacheDeviceTopics();
            await InitializeSubscribersAsync();
            UpdateConnectionStatus();
        }

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
                    MessageBox.Show($"ZeroMQ Publisher 초기화 실패: {ex.Message}",
                        "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show($"MQTT 초기화 실패: {ex.Message}",
                        "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _mqttPublisher = null;
                }
            }

            UpdatePublisherStatus();
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
                    MessageBox.Show($"ZeroMQ Subscriber 초기화 실패: {ex.Message}",
                        "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show($"MQTT Subscriber 초기화 실패: {ex.Message}",
                        "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _mqttSubscriber = null;
                }
            }
        }

        private void OnMessageReceived(string topic, string message)
        {
            if (InvokeRequired)
            {
                // Use BeginInvoke to avoid deadlock - fire-and-forget UI marshaling
                BeginInvoke(new Action<string, string>(OnMessageReceived), topic, message);
                return;
            }

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

                // Whitelist validation: only allow writes to registered device addresses
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

                // Bit device value validation: only allow 0 or 1
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

                // Execute PLC write on background thread to avoid blocking UI
                int valueToWrite = command.Value;
                Task.Run(() => ExecutePlcWrite(address, valueToWrite, targetDevice));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ProcessWriteCommand exception: {ex.Message}");
            }
        }

        private void ExecutePlcWrite(string address, int value, DeviceItem targetDevice)
        {
            try
            {
                int result = _plcManager.WriteDevice(address, value);
                if (result == 0)
                {
                    // Marshal UI update back to UI thread
                    BeginInvoke(new Action(() =>
                    {
                        targetDevice.Value = value;
                        if (WindowState != FormWindowState.Minimized)
                            dgvDevices.Refresh();
                    }));
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
            // topic format: "plc/D0/set" -> "D0"
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            tmrMonitor.Stop();
            _plcManager?.Dispose();
            _zmqPublisher?.Dispose();
            _mqttPublisher?.Dispose();
            _zmqSubscriber?.Dispose();
            _mqttSubscriber?.Dispose();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            int result = _plcManager.Connect();
            if (result == 0)
            {
                tmrMonitor.Start();
                UpdateConnectionStatus();
            }
            else
            {
                MessageBox.Show($"PLC 연결 실패\n{PlcManager.GetErrorMessage(result)}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            tmrMonitor.Stop();
            int result = _plcManager.Disconnect();
            if (result != 0)
            {
                MessageBox.Show($"PLC 연결 해제 실패\n{PlcManager.GetErrorMessage(result)}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            UpdateConnectionStatus();
        }

        private void tmrMonitor_Tick(object sender, EventArgs e)
        {
            if (!_plcManager.IsConnected)
            {
                // Attempt auto-reconnect
                if (!TryAutoReconnect())
                {
                    return;
                }
            }

            bool success = _plcManager.ReadDevices(new List<DeviceItem>(_deviceItems));

            if (!success)
            {
                // Connection lost during read - attempt reconnect on next tick
                System.Diagnostics.Debug.WriteLine("[WARN] PLC read failed, will attempt reconnect");
                UpdateConnectionStatus();
                return;
            }

            // Reset reconnect counter on successful read
            _reconnectAttempts = 0;

            if (WindowState != FormWindowState.Minimized)
                dgvDevices.Refresh();

            PublishDeviceValues();
        }

        private bool TryAutoReconnect()
        {
            // Check if we've exceeded max attempts
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                tmrMonitor.Stop();
                UpdateConnectionStatus();
                System.Diagnostics.Debug.WriteLine($"[ERROR] Auto-reconnect failed after {MAX_RECONNECT_ATTEMPTS} attempts");
                MessageBox.Show($"PLC 연결이 끊어졌습니다.\n자동 재연결 {MAX_RECONNECT_ATTEMPTS}회 시도 후 실패했습니다.\n수동으로 연결 버튼을 눌러주세요.",
                    "연결 끊김", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Check reconnect interval
            var now = DateTime.Now;
            if ((now - _lastReconnectAttempt).TotalMilliseconds < RECONNECT_INTERVAL_MS)
            {
                return false;
            }

            _lastReconnectAttempt = now;
            _reconnectAttempts++;

            System.Diagnostics.Debug.WriteLine($"[INFO] Auto-reconnect attempt {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}");

            int result = _plcManager.Reconnect();
            if (result == 0)
            {
                _reconnectAttempts = 0;
                UpdateConnectionStatus();
                System.Diagnostics.Debug.WriteLine("[INFO] Auto-reconnect successful");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[WARN] Auto-reconnect failed: {PlcManager.GetErrorMessage(result)}");
            UpdateConnectionStatus();
            return false;
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

        private void dgvDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvDevices.Columns["colWrite"].Index)
                return;

            if (!_plcManager.IsConnected)
            {
                MessageBox.Show("PLC에 연결되지 않았습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var device = _deviceItems[e.RowIndex];
            ShowWriteDialog(device);
        }

        private void ShowWriteDialog(DeviceItem device)
        {
            string currentValue = device.Type == "Bit"
                ? (device.Value == 0 ? "0 (OFF)" : "1 (ON)")
                : device.Value.ToString();

            string message = $"주소: {device.Address}\n현재 값: {currentValue}\n\n새 값을 입력하세요:";

            if (device.Type == "Bit")
            {
                message += "\n(0=OFF, 1=ON)";
            }

            using (var inputForm = new Form())
            {
                inputForm.Text = $"{device.Name} 값 쓰기";
                inputForm.Size = new Size(300, 180);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var lblMessage = new Label
                {
                    Text = message,
                    Location = new Point(12, 12),
                    AutoSize = true
                };

                var txtValue = new TextBox
                {
                    Location = new Point(12, 85),
                    Size = new Size(260, 23),
                    Text = device.Value.ToString()
                };

                var btnOk = new Button
                {
                    Text = "확인",
                    DialogResult = DialogResult.OK,
                    Location = new Point(116, 115),
                    Size = new Size(75, 25)
                };

                var btnCancel = new Button
                {
                    Text = "취소",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(197, 115),
                    Size = new Size(75, 25)
                };

                inputForm.Controls.AddRange(new Control[] { lblMessage, txtValue, btnOk, btnCancel });
                inputForm.AcceptButton = btnOk;
                inputForm.CancelButton = btnCancel;

                if (inputForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (int.TryParse(txtValue.Text, out int newValue))
                    {
                        if (device.Type == "Bit" && newValue != 0 && newValue != 1)
                        {
                            MessageBox.Show("비트 타입은 0 또는 1만 입력 가능합니다.",
                                "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int result = _plcManager.WriteDevice(device.Address, newValue);
                        if (result == 0)
                        {
                            device.Value = newValue;
                            dgvDevices.Refresh();
                        }
                        else
                        {
                            MessageBox.Show($"값 쓰기 실패\n{PlcManager.GetErrorMessage(result)}",
                                "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("유효한 숫자를 입력하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void UpdateConnectionStatus()
        {
            bool connected = _plcManager?.IsConnected ?? false;
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;

            if (connected)
            {
                lblStatus.Text = "연결됨";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = "연결 안됨";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void UpdatePublisherStatus()
        {
            bool zmqConnected = _zmqPublisher?.IsConnected ?? false;
            lblZmqStatus.Text = zmqConnected ? "ZMQ: 연결됨" : "ZMQ: 연결 안됨";
            lblZmqStatus.ForeColor = zmqConnected ? Color.Green : Color.Gray;

            bool mqttConnected = _mqttPublisher?.IsConnected ?? false;
            lblMqttStatus.Text = mqttConnected ? "MQTT: 연결됨" : "MQTT: 연결 안됨";
            lblMqttStatus.ForeColor = mqttConnected ? Color.Green : Color.Gray;
        }
    }
}
