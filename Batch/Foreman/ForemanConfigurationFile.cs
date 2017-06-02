using Batch.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    // Default configuration

    public class ForemanConfigurationFile
    {
        public string foremanId = "";
        public string foremanVer = "0.1.1";

        public List<FCFAssemblyFile> assemblies;
        public List<FCFNode> nodes;
        public List<FCFQueue> queues;

        // connections define which node is passing data to which node
        public List<FCFConnection> connections;
    }

    public class FCFAssemblyFile
    {
        public string name = "";
        public string path;
    }

    public class FCFNode
    {
        public int id;
        public string name;
        public string assemblyName;
        public string className;
        public int exeOrderId;
        public bool isWaitToFinish = true;
    }

    public class FCFQueue
    {
        public int id;
        public string name;
        public int bufferLimit = 0;
    }

    public class FCFConnection
    {
        public string from;
        public string to;
    }
}
