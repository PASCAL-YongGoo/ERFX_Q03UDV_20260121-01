using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ERFX_Q03UDV_20260121_01
{
    public partial class Form1 : Form
    {
        private PlcMonitorService _service;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            string configPath = Path.Combine(Application.StartupPath, "config.json");
            _service = new PlcMonitorService(configPath);

            // Subscribe to service events
            _service.DeviceValuesChanged += OnDeviceValuesChanged;
            _service.PlcConnectionStateChanged += OnPlcConnectionStateChanged;
            _service.PublisherStateChanged += OnPublisherStateChanged;
            _service.ErrorOccurred += OnErrorOccurred;
            _service.AutoReconnectFailed += OnAutoReconnectFailed;

            // Bind data
            dgvDevices.DataSource = _service.DeviceItems;

            // Initialize
            await _service.InitializeAsync();
            lblInterval.Text = $"갱신 주기: {_service.MonitoringIntervalMs}ms";
            UpdateConnectionStatus();
            UpdatePublisherStatus();
        }

        #region Event Handlers - Service Events

        private void OnDeviceValuesChanged()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnDeviceValuesChanged));
                return;
            }

            if (WindowState != FormWindowState.Minimized)
                dgvDevices.Invalidate();
        }

        private void OnPlcConnectionStateChanged(bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(OnPlcConnectionStateChanged), connected);
                return;
            }

            UpdateConnectionStatus();
        }

        private void OnPublisherStateChanged(bool zmqConnected, bool mqttConnected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, bool>(OnPublisherStateChanged), zmqConnected, mqttConnected);
                return;
            }

            UpdatePublisherStatus();
        }

        private void OnErrorOccurred(string title, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(OnErrorOccurred), title, message);
                return;
            }

            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OnAutoReconnectFailed()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnAutoReconnectFailed));
                return;
            }

            UpdateConnectionStatus();
            MessageBox.Show(
                "PLC 연결이 끊어졌습니다.\n자동 재연결 3회 시도 후 실패했습니다.\n수동으로 연결 버튼을 눌러주세요.",
                "연결 끊김", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region Event Handlers - UI Events

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _service?.Dispose();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (_service.Connect())
            {
                _service.StartMonitoring();
                UpdateConnectionStatus();
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            _service.StopMonitoring();
            _service.Disconnect();
            UpdateConnectionStatus();
        }

        private void tmrMonitor_Tick(object sender, EventArgs e)
        {
            // Timer is no longer used - monitoring loop is in PlcMonitorService
        }

        private void dgvDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvDevices.Columns["colWrite"].Index)
                return;

            if (!_service.IsPlcConnected)
            {
                MessageBox.Show("PLC에 연결되지 않았습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var device = _service.GetDevice(e.RowIndex);
            if (device != null)
            {
                ShowWriteDialog(device);
            }
        }

        #endregion

        #region UI Methods

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
                        var (success, errorMessage) = _service.WriteDevice(device.Address, newValue);
                        if (success)
                        {
                            dgvDevices.Refresh();
                        }
                        else
                        {
                            MessageBox.Show(errorMessage, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            bool connected = _service?.IsPlcConnected ?? false;
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
            bool zmqConnected = _service?.IsZmqConnected ?? false;
            lblZmqStatus.Text = zmqConnected ? "ZMQ: 연결됨" : "ZMQ: 연결 안됨";
            lblZmqStatus.ForeColor = zmqConnected ? Color.Green : Color.Gray;

            bool mqttConnected = _service?.IsMqttConnected ?? false;
            lblMqttStatus.Text = mqttConnected ? "MQTT: 연결됨" : "MQTT: 연결 안됨";
            lblMqttStatus.ForeColor = mqttConnected ? Color.Green : Color.Gray;
        }

        #endregion
    }
}
