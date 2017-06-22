using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BatchAgent
{
    [DataContract]
    public class ForemanStats
    {
        [DataMember]
        public string ForemanId;

        [DataMember]
        public long AppDomainMonitoringSurvivedMemorySize;

        [DataMember]
        public long AppDomainMonitoringSurvivedProcessMemorySize;

        [DataMember]
        public long AppDomainMonitoringTotalAllocatedMemorySize;

        [DataMember]
        public TimeSpan AppDomainMonitoringTotalProcessorTime;

        [DataMember]
        public bool IsError;

        [DataMember]
        public string WorkerNodeExceptionString;
    }
}