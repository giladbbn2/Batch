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
        public bool IsAppDomainMonitoringEnabled
        {
            get
            {
                return true;
            }
        }

        public string ForemanFetchDLLBaseDir = null;

        // if this is true then Batch copies the DLL file to a a local directory (to ForemanLocalDLLBaseDir)
        public bool IsKeepLocalForemanDLL = false;

        public bool IsOverwriteLocalForemanDLL = false;

        public string ForemanLocalDLLBaseDir = null;
    }
}
