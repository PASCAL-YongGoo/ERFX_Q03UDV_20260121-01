using System.Runtime.Serialization;

namespace ERFX_Q03UDV_20260121_01
{
    [DataContract]
    public class PublishMessage
    {
        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "value")]
        public int Value { get; set; }

        [DataMember(Name = "timestamp")]
        public string Timestamp { get; set; }
    }
}
