using System.Runtime.Serialization;

namespace ERFX_Q03UDV_20260121_01
{
    [DataContract]
    public class WriteCommand
    {
        [DataMember(Name = "value")]
        public int Value { get; set; }
    }
}
