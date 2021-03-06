﻿using Batch.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    // Default configuration

    [Serializable]
    internal sealed class ForemanConfigurationFile
    {
        public string foremanVer = "0.1";
        public bool isNodesLongRunning = false;
        public string assemblyPath = "";
        
        public List<FCFNode> nodes;
        public List<FCFQueue> queues;

        // connections define which node is passing data to which node and node to queue or queue to node
        public List<FCFConnection> connections;
    }

    [Serializable]
    internal sealed class FCFNode
    {
        public int id;
        public string name;
        public string className;
        public int exeOrderId = 1;
    }

    [Serializable]
    internal sealed class FCFQueue
    {
        public int id;
        public string name;
        public int bufferLimit = 0;
    }

    [Serializable]
    internal sealed class FCFConnection
    {
        public string from;
        public string to;
    }
}
