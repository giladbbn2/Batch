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

        private WorkerActivator WorkerActivator;
        private ForemanConfiguration Config;

        private WorkerNode[] Nodes; // id is nodeId
        private BlockingCollection<object>[] Queues;    // id is queueId

        // helper structures
        private Dictionary<string, int> WorkerNameToId;
        private Dictionary<string, int> NodeNameToId;
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
            int NodeCounter = 0;
            int QueueCounter = 0;

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

            Nodes = new WorkerNode[Config.nodes.Count];
            NodeNameToId = new Dictionary<string, int>(Config.nodes.Count);
            foreach (var configNode in Config.nodes)
            {
                string workerName = configNode.worker;
                if (!WorkerNameToId.ContainsKey(workerName))
                    throw new ArgumentException("The worker name '" + workerName + "' in nodes section is not defined in workers section");
                int workerId = WorkerNameToId[workerName];
                
                if (NodeNameToId.ContainsKey(configNode.name))
                    throw new ArgumentException("The node name '" + configNode.name + "' is already registered");

                var node = new WorkerNode();
                node.Id = NodeCounter;
                node.Name = configNode.name;
                node.WorkerId = workerId;
                node.WorkerActivator = WorkerActivator;
                Nodes[NodeCounter] = node;
                NodeNameToId.Add(configNode.name, node.Id);

                NodeCounter++;
            }

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

                int fromElId, toElId;

                TopologyElementType fromEl = GetNameTopologyType(fromName, out fromElId);
                TopologyElementType toEl = GetNameTopologyType(toName, out toElId);

                if (fromEl == TopologyElementType.None && toEl == TopologyElementType.None)
                    throw new Exception("Connection from and to elements do not exist: '" + fromName + "' -> '" + toName + "'");

                // node to node, node to queue and queue to node are supported
                // queue to queue is not supported
                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Queue)
                    throw new Exception("Can't connect a queue to a queue: '" + fromName + "' -> '" + toName + "'");

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Queue)
                {
                    var node = Nodes[fromElId];
                    var queue = Queues[toElId];

                    if (node.Output != null)
                        throw new Exception("Can't set two output elements for same node: '" + fromName + "' -> '" + toName + "'");
                    
                    node.Output = queue;
                    node.IsConnected = true;
                    QueueIsToEl[toElId] = true;
                }

                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Node)
                {
                    var node = Nodes[toElId];
                    var queue = Queues[fromElId];

                    if (node.Input != null)
                        throw new Exception("Can't set two input elements for the same node: '" + fromName + "' -> '" + toName + "'");

                    node.Input = queue;
                    node.IsConnected = true;
                    QueueIsFromEl[fromElId] = true;
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
            Task[] tasks = new Task[Nodes.Length];
            
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

        private TopologyElementType GetNameTopologyType(string Name, out int id)
        {
            int nodeId, queueId, workerId;

            bool isNode = NodeNameToId.TryGetValue(Name, out nodeId);
            bool isQueue = QueueNameToId.TryGetValue(Name, out queueId);

            if (isNode && isQueue)
                throw new Exception("The name '" + Name + "' is ambigious - a node or a queue?");

            bool isWorker = WorkerNameToId.TryGetValue(Name, out workerId);

            if ((isNode && isWorker) || (isQueue && isWorker))
                throw new Exception("The name '" + Name + "' is ambigious");

            if (isNode)
            {
                id = nodeId;
                return TopologyElementType.Node;
            }

            if (isQueue)
            {
                id = queueId;
                return TopologyElementType.Queue;
            }

            if (isWorker)
            {
                id = workerId;
                return TopologyElementType.Worker;
            }

            id = -1;
            return TopologyElementType.None;
        }
    }
}
