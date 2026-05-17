using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class AckResponse
    {
        [DataMember]
        public ResponseStatus Status { get; set; }

        [DataMember]
        public string Message { get; set; }

        public AckResponse() { }

        public AckResponse(ResponseStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Status}] {Message}";
        }
    }
}
