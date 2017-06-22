using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    [Serializable]
    public class ContractorSettings
    {
        private bool _IsAppDomainMonitoringEnabled = true;
        public bool IsAppDomainMonitoringEnabled
        {
            get
            {
                return _IsAppDomainMonitoringEnabled;
            }
            internal set
            {
                _IsAppDomainMonitoringEnabled = value;
            }
        }

        public string ForemanDllBaseDir = null;
    }
}
