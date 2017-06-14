﻿using Batch.Foreman;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    [Serializable]
    internal class ContractorConfigurationFile
    {
        public string contractorVer = "0.1";

        public List<CCFForeman> foremen;
        public List<CCFConnection> connections;
    }

    [Serializable]
    internal class CCFConnection
    {
        public string from;
        public string to;
        public bool IsTestForeman = false;
        public int TestForemanRequestWeight = 1000000;
    }

    [Serializable]
    internal class CCFForeman
    {
        public string id;
        public ForemanConfigurationFile config;
    }
}
