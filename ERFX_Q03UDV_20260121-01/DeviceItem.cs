using System;
using System.ComponentModel;

namespace ERFX_Q03UDV_20260121_01
{
    public class DeviceItem : INotifyPropertyChanged
    {
        private string _name;
        private string _address;
        private string _type;
        private int _value;

        // Cached topic strings for publishing
        public string ZmqTopic { get; set; }
        public string MqttTopic { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged(nameof(Address));
                }
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(DisplayValue));
                }
            }
        }

        public string DisplayValue
        {
            get
            {
                if (Type == "Bit")
                {
                    return _value == 0 ? "OFF" : "ON";
                }
                return _value.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
