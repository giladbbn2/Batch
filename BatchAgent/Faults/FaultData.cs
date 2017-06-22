using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BatchAgent
{
    [DataContract]
    public class FaultData
    {
        public FaultData() { }

        public FaultData(string Message, string DetailedInformation)
        {
            this.Message = Message;
            this.DetailedInformation = DetailedInformation;
        }

        [DataMember]
        public string Message { get; private set; }

        [DataMember]
        public string DetailedInformation { get; private set; }
    }
}