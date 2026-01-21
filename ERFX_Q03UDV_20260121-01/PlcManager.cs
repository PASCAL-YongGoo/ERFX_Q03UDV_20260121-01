using System;
using System.Collections.Generic;
using ActUtlTypeLib;

namespace ERFX_Q03UDV_20260121_01
{
    public class PlcManager : IDisposable
    {
        private readonly object _lock = new object();
        private ActUtlType _plc;
        private int _stationNumber;
        private bool _isConnected;
        private bool _disposed;

        public bool IsConnected
        {
            get { lock (_lock) { return _isConnected; } }
        }
        public int StationNumber => _stationNumber;

        // Error codes indicating connection loss
        private const int ERR_TIMEOUT = 0x01800010;
        private const int ERR_CONNECTION_FAILED = 0x01800011;
        private const int ERR_NOT_OPEN = 0x01800005;

        public PlcManager(int stationNumber)
        {
            _stationNumber = stationNumber;
            _plc = new ActUtlType();
            _plc.ActLogicalStationNumber = _stationNumber;
            _isConnected = false;
        }

        public int Connect()
        {
            lock (_lock)
            {
                if (_isConnected)
                    return 0;

                int result = _plc.Open();
                _isConnected = (result == 0);
                return result;
            }
        }

        public int Disconnect()
        {
            lock (_lock)
            {
                if (!_isConnected)
                    return 0;

                int result = _plc.Close();
                if (result == 0)
                {
                    _isConnected = false;
                }
                return result;
            }
        }

        public int Reconnect()
        {
            lock (_lock)
            {
                if (_isConnected)
                {
                    _plc.Close();
                    _isConnected = false;
                }

                int result = _plc.Open();
                _isConnected = (result == 0);
                return result;
            }
        }

        public int ReadDevice(string address, out int value)
        {
            value = 0;
            lock (_lock)
            {
                if (!_isConnected)
                    return -1;

                int result = _plc.GetDevice(address, out value);
                CheckConnectionLoss(result);
                return result;
            }
        }

        public int WriteDevice(string address, int value)
        {
            lock (_lock)
            {
                if (!_isConnected)
                    return -1;

                int result = _plc.SetDevice(address, value);
                CheckConnectionLoss(result);
                return result;
            }
        }

        /// <summary>
        /// Reads multiple devices. Returns true if all reads succeeded, false if connection lost.
        /// </summary>
        public bool ReadDevices(List<DeviceItem> devices)
        {
            lock (_lock)
            {
                if (!_isConnected || devices == null)
                    return false;

                foreach (var device in devices)
                {
                    int value;
                    int result = _plc.GetDevice(device.Address, out value);
                    if (result == 0)
                    {
                        device.Value = value;
                    }
                    else if (IsConnectionLostError(result))
                    {
                        _isConnected = false;
                        return false;
                    }
                }
                return true;
            }
        }

        public int ReadDeviceBlock(string startAddress, int count, out int[] values)
        {
            values = new int[count];
            lock (_lock)
            {
                if (!_isConnected)
                    return -1;

                int result = _plc.ReadDeviceBlock(startAddress, count, out values[0]);
                CheckConnectionLoss(result);
                return result;
            }
        }

        public int WriteDeviceBlock(string startAddress, int count, int[] values)
        {
            lock (_lock)
            {
                if (!_isConnected || values == null || values.Length < count)
                    return -1;

                int result = _plc.WriteDeviceBlock(startAddress, count, ref values[0]);
                CheckConnectionLoss(result);
                return result;
            }
        }

        private bool IsConnectionLostError(int errorCode)
        {
            return errorCode == ERR_TIMEOUT ||
                   errorCode == ERR_CONNECTION_FAILED ||
                   errorCode == ERR_NOT_OPEN;
        }

        private void CheckConnectionLoss(int errorCode)
        {
            if (IsConnectionLostError(errorCode))
            {
                _isConnected = false;
            }
        }

        public static string GetErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 0:
                    return "정상";
                case 0x01800001:
                    return "설정 파일 없음";
                case 0x01800002:
                    return "설정 파일 읽기 오류";
                case 0x01800003:
                    return "메모리 부족";
                case 0x01800004:
                    return "이미 열려 있음";
                case 0x01800005:
                    return "열려 있지 않음";
                case 0x01800006:
                    return "동기화 오류";
                case 0x01800010:
                    return "통신 타임아웃";
                case 0x01800011:
                    return "연결 실패";
                default:
                    return $"오류 코드: 0x{errorCode:X8}";
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                    if (_isConnected)
                    {
                        try
                        {
                            _plc.Close();
                        }
                        catch
                        {
                            // Ignore errors during dispose
                        }
                        _isConnected = false;
                    }
                }
                _disposed = true;
            }
        }

        ~PlcManager()
        {
            Dispose(false);
        }
    }
}
