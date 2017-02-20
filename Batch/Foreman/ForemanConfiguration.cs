using Batch.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    // Default configuration

    internal class ForemanConfiguration
    {
        public string foremanVer = "0.1";
        public string NetMQPort = "5556";

        public List<FCWorker> workers;
        public List<FCNode> nodes;
        public List<FCQueue> queues;
        public List<FCConnection> connections;
    }

    public class FCWorker
    {
        public int id;
        public string name;
        public string className;
    }

    public class FCNode
    {
        public int id;
        public string name;
        public string worker;
        public bool isFirst;
        public bool isLast;
        public int nodeCount = 1;
    }

    public class FCQueue
    {
        public int id;
        public string name;
        public int bufferLimit = 0;
    }

    public class FCConnection
    {
        public string from;
        public string to;
    }
}
