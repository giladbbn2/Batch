using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    public class ContractorConfigurationFile
    {
        public string contractorId = "";
        public string contractorVer = "0.1.1";
        public int NetMQPort = 5556;

        public List<CCFForman> foremen;

        
    }

    public class CCFForman
    {
        public string configFile;
    }
}
