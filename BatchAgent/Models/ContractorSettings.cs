using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BatchAgent
{
    [DataContract]
    public class ContractorSettings
    {
        [DataMember]
        public bool IsAppDomainMonitoringEnabled;

        [DataMember]
        public string ForemanFetchDLLBaseDir;

        [DataMember]
        public bool IsKeepLocalForemanDLL = false;

        [DataMember]
        public bool IsOverwriteLocalForemanDLL = false;

        [DataMember]
        public string ForemanLocalDLLBaseDir = null;
    }
}