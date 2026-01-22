using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ERFX_Q03UDV_20260121_01
{
    /// <summary>
    /// R5050PMG 바코드 리더에 TCP 연결하여 트리거 신호를 전송하는 클라이언트
    /// </summary>
    public class BarcodeReaderClient : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private CancellationTokenSource _cts;
        private Task _reconnectTask;
        private bool _disposed;

        private readonly string _ipAddress;
        private readonly int _port;
        private readonly int _connectionTimeoutMs;
        private readonly bool _autoReconnect;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected => _tcpClient?.Connected == true && _networkStream != null;

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        public event EventHandler<bool> ConnectionStateChanged;

        /// <summary>
        /// 에러 발생 이벤트
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        public BarcodeReaderClient(string ipAddress, int port, int connectionTimeoutMs, bool autoReconnect)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            _port = port;
            _connectionTimeoutMs = connectionTimeoutMs;
            _autoReconnect = autoReconnect;
        }

        /// <summary>
        /// 바코드 리더에 연결을 시작합니다.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (IsConnected)
                    return true;

                CloseConnection();

                _tcpClient = new TcpClient();
                _cts = new CancellationTokenSource();

                var connectTask = _tcpClient.ConnectAsync(_ipAddress, _port);
                var timeoutTask = Task.Delay(_connectionTimeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (completedTask == timeoutTask)
                {
                    CloseConnection();
                    RaiseError($"연결 타임아웃: {_ipAddress}:{_port}");
                    return false;
                }

                await connectTask.ConfigureAwait(false);
                _networkStream = _tcpClient.GetStream();

                RaiseConnectionStateChanged(true);
                return true;
            }
            catch (Exception ex)
            {
                CloseConnection();
                RaiseError($"연결 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 바코드 리더 연결을 닫습니다.
        /// </summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            CloseConnection();
            RaiseConnectionStateChanged(false);
        }

        /// <summary>
        /// 바코드 리더에 트리거 신호를 전송합니다.
        /// </summary>
        /// <param name="command">전송할 명령 (기본값: "+")</param>
        public async Task<bool> SendTriggerAsync(string command = "+")
        {
            if (!IsConnected)
            {
                // 자동 재연결 시도
                if (_autoReconnect)
                {
                    if (!await ConnectAsync().ConfigureAwait(false))
                        return false;
                }
                else
                {
                    RaiseError("바코드 리더가 연결되지 않았습니다.");
                    return false;
                }
            }

            try
            {
                var data = Encoding.UTF8.GetBytes(command);
                await _networkStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"트리거 전송 실패: {ex.Message}");
                CloseConnection();

                // 자동 재연결 시도
                if (_autoReconnect && _reconnectTask == null)
                {
                    _reconnectTask = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        await ConnectAsync().ConfigureAwait(false);
                        _reconnectTask = null;
                    });
                }

                return false;
            }
        }

        private void CloseConnection()
        {
            try
            {
                _networkStream?.Close();
                _networkStream?.Dispose();
                _networkStream = null;

                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;
            }
            catch { }
        }

        private void RaiseConnectionStateChanged(bool isConnected)
        {
            ConnectionStateChanged?.Invoke(this, isConnected);
        }

        private void RaiseError(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            CloseConnection();
        }
    }
}
