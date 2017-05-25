using Batch.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    // Default configuration

    internal class ForemanConfigurationFile
    {
        public string foremanId = "";
        public string foremanVer = "0.1.1";

        public List<FCFWorker> workers;
        public List<FCFNode> nodes;
        public List<FCFQueue> queues;
        public List<FCFConnection> connections;
    }

    public class FCFWorker
    {
        public int id;
        public string name;
        public string className;
    }

    public class FCFNode
    {
        public int id;
        public string name;
        public string worker;
        public int orderId;
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
