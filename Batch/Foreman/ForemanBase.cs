using Batch.Worker;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

//[assembly: log4net.Config.XmlConfigurator(ConfigFile = "MyStandardLog4Net.config", Watch = true)]

namespace Batch.Foreman
{
    public abstract class ForemanBase : IDisposable
    {
        public string PathToSettingsFile;

        public BlockingCollection<object> firstQueue;
        public BlockingCollection<object> lastQueue;

        private WorkerActivator WorkerActivator;
        private ForemanConfiguration Config;

        private List<WorkerNode> Nodes; // id is nodeId
        private BlockingCollection<object>[] Queues;    // id is queueId

        private List<int> firstNodeIds;
        private List<int> lastNodeIds;

        // helper structures
        private Dictionary<string, int> WorkerNameToId;
        private Dictionary<string, List<int>> NodeNameToId;
        private Dictionary<string, int> QueueNameToId;
        private bool[] QueueIsToEl;   // id is queueId
        private bool[] QueueIsFromEl; // id is queueId

        private bool Disposed;


        public ForemanBase()
        {
            Config = new ForemanConfiguration();
        }

        public ForemanBase(string PathToSettingsFile)
        {
            this.PathToSettingsFile = PathToSettingsFile;
            Config = new ForemanConfiguration();
        }

        public void LoadSettingsFile(string PathToSettingsFile)
        {
            this.PathToSettingsFile = PathToSettingsFile;
            LoadSettingsFile();
        }

        public void LoadSettingsFile()
        {
            if (Disposed)
                return;

            if (PathToSettingsFile == null)
                throw new ArgumentNullException("PathToSettingsFile");

            if (!File.Exists(PathToSettingsFile))
                throw new FileNotFoundException(PathToSettingsFile);

            int WorkerCounter = 0;
            int QueueCounter = 0;
            bool isGlobalIsFirst = false;
            bool isGlobalIsLast = false;
            firstNodeIds = new List<int>();
            lastNodeIds = new List<int>();
            firstQueue = new BlockingCollection<object>();
            lastQueue = new BlockingCollection<object>();
            WorkerActivator = new WorkerActivator();
            
            try
            {
                string settings = File.ReadAllText(PathToSettingsFile);
                Config = JsonConvert.DeserializeObject<ForemanConfiguration>(settings);
            }
            catch (Exception ex)
            {
                throw new Exception("Can't parse config file: " + PathToSettingsFile + "(" + ex.Message + ")");
            }

            // Register workers
            if (Config.workers == null || Config.workers.Count == 0)
                throw new ArgumentException("No workers in config file");

            WorkerNameToId = new Dictionary<string, int>(Config.workers.Count);
            foreach (var configWorker in Config.workers)
            {
                if (WorkerNameToId.ContainsKey(configWorker.name))
                    throw new ArgumentException("The worker name '" + configWorker.name + "' is already registered");

                int workerId = WorkerCounter;
                WorkerCounter++;

                try
                {
                    WorkerActivator.RegisterWorkerType(workerId, configWorker.className);
                    WorkerNameToId.Add(configWorker.name, workerId);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Can't create a worker using className (" + ex.Message + ")");
                }
            }

            // Register nodes
            if (Config.nodes == null || Config.nodes.Count == 0)
                throw new ArgumentException("No nodes in config file");

            Nodes = new List<WorkerNode>();
            NodeNameToId = new Dictionary<string, List<int>>();
            foreach (var configNode in Config.nodes)
            {
                string workerName = configNode.worker;
                if (!WorkerNameToId.ContainsKey(workerName))
                    throw new ArgumentException("The worker name '" + workerName + "' in nodes section is not defined in workers section");
                int workerId = WorkerNameToId[workerName];
                
                if (NodeNameToId.ContainsKey(configNode.name))
                    throw new ArgumentException("The node name '" + configNode.name + "' is already registered");

                List<int> ids = new List<int>(configNode.nodeCount);

                for (int i=0; i<configNode.nodeCount; i++)
                {
                    var node = new WorkerNode();
                    node.Id = Nodes.Count;
                    node.Name = configNode.name;
                    node.WorkerId = workerId;
                    node.Guid = Guid.NewGuid().ToString();
                    node.WorkerActivator = WorkerActivator;

                    if (configNode.isFirst)
                    {
                        isGlobalIsFirst = true;
                        node.IsFirst = true;
                        firstNodeIds.Add(node.Id);
                    }

                    if (configNode.isLast)
                    {
                        isGlobalIsLast = true;
                        node.IsLast = true;
                        lastNodeIds.Add(node.Id);
                    }

                    Nodes.Add(node);
                    ids.Add(node.Id);
                }

                NodeNameToId.Add(configNode.name, ids);
            }

            if (!isGlobalIsFirst)
                throw new ArgumentException("There is no first node");

            if (!isGlobalIsLast)
                throw new ArgumentException("There is no last node");

            // Register queues
            if (Config.queues == null || Config.queues.Count == 0)
                throw new ArgumentException("No queues in config file");

            Queues = new BlockingCollection<object>[Config.queues.Count];
            QueueNameToId = new Dictionary<string, int>(Config.queues.Count);
            QueueIsToEl = new bool[Config.queues.Count];
            QueueIsFromEl = new bool[Config.queues.Count];
            foreach (var configQueue in Config.queues)
            {
                if (QueueNameToId.ContainsKey(configQueue.name))
                    throw new ArgumentException("The queue name '" + configQueue.name + "' is already registered");

                int queueId = QueueCounter;

                if (configQueue.bufferLimit == 0)
                    Queues[queueId] = new BlockingCollection<object>();
                else
                    Queues[queueId] = new BlockingCollection<object>(configQueue.bufferLimit);

                QueueNameToId.Add(configQueue.name, queueId);

                QueueCounter++;
            }

            // Register connections
            foreach (var configConnection in Config.connections)
            {
                string fromName = configConnection.from;
                string toName = configConnection.to;

                List<int> fromNodeIds, toNodeIds;
                int fromQueueId, toQueueId, fromWorkerId, toWorkerId;

                TopologyElementType fromEl = GetNameTopologyType(fromName, out fromNodeIds, out fromQueueId, out fromWorkerId);
                TopologyElementType toEl = GetNameTopologyType(toName, out toNodeIds, out toQueueId, out toWorkerId);

                // node to queue and queue to node are supported
                // queue to queue is not supported, node to node is not supported

                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Queue)
                    throw new Exception("Can't connect a queue to a queue: '" + fromName + "' -> '" + toName + "'");

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Node)
                    throw new Exception("Can't connect a node to a node: '" + fromName + "' -> '" + toName + "'");

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Queue)
                {
                    var queue = Queues[toQueueId];
                    for (int i=0; i<fromNodeIds.Count; i++)
                    {
                        var node = Nodes[fromNodeIds[i]];

                        if (i==0 && node.Output != null)
                            throw new Exception("Can't set two output elements for same node: '" + fromName + "' -> '" + toName + "'");

                        node.Output = queue;
                        node.IsConnected = true;

                        if (node.IsFirst)
                            node.Input = firstQueue;
                    }

                    QueueIsToEl[toQueueId] = true;
                }

                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Node)
                {
                    var queue = Queues[fromQueueId];
                    for (int i=0; i<toNodeIds.Count; i++)
                    {
                        var node = Nodes[toNodeIds[i]];

                        if (i==0 && node.Input != null)
                            throw new Exception("Can't set two input elements for the same node: '" + fromName + "' -> '" + toName + "'");

                        node.Input = queue;
                        node.IsConnected = true;

                        if (node.IsLast)
                            node.Output = lastQueue;
                    }

                    QueueIsFromEl[fromQueueId] = true;
                }
            }

            DryRun();

            // dispose of helpers
            WorkerNameToId = null;
            NodeNameToId = null;
            QueueNameToId = null;
        }

        public void DryRun()
        {
            // verify connections

            // iterate over all tree and check if there are any unconnected nodes
            if (Nodes.Count > 0)
                foreach (var node in Nodes)
                    if (!node.IsConnected)
                        throw new Exception("Node is not connected to topology tree: '" + node.Name + "'");

            // check a queue is not an edge
            for (var i = 0; i < Queues.Length; i++)
            {
                if (!QueueIsFromEl[i] || !QueueIsToEl[i])
                    throw new Exception("A queue cannot be an edge, but must connect a node as input and another node as output");
            }

            // several independent topologies can coexist in a single foreman

            // dispose of helpers
            QueueIsFromEl = null;
            QueueIsToEl = null;
        }

        public void Run()
        {
            var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            Task[] tasks = new Task[Nodes.Count];
            
            foreach (var node in Nodes)
            {
                Console.WriteLine("Starting new thread with " + node.Name);
                tasks[node.Id] = f.StartNew(() => node.Run());
            }

            Task.WaitAll(tasks);    
        }

        public void Dispose()
        {
            Disposed = true;
        }

        private TopologyElementType GetNameTopologyType(string Name, out List<int> NodeIds, out int QueueId, out int WorkerId)
        {
            bool isNode = NodeNameToId.TryGetValue(Name, out NodeIds);
            bool isQueue = QueueNameToId.TryGetValue(Name, out QueueId);

            if (isNode && isQueue)
                throw new Exception("The name '" + Name + "' is ambigious - a node or a queue?");

            bool isWorker = WorkerNameToId.TryGetValue(Name, out WorkerId);

            if ((isNode && isWorker) || (isQueue && isWorker))
                throw new Exception("The name '" + Name + "' is ambigious");

            if (isNode)
                return TopologyElementType.Node;

            if (isQueue)
                return TopologyElementType.Queue;

            if (isWorker)
                return TopologyElementType.Worker;

            return TopologyElementType.None;
        }
    }
}
