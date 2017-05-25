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

namespace Batch.Foreman
{
    public abstract class ForemanBase : IDisposable
    {
        public string Id;

        public string PathToSettingsFile;

        private WorkerActivator WorkerActivator;
        private ForemanConfigurationFile Config;

        private WorkerNode[] Nodes;                                 // id is nodeId
        private WorkerNodeState[] NodeState;                        // id is nodeId
        private Dictionary<int, List<WorkerNode>> WorkerNodeOrder;  // id is orderId
        private BlockingCollection<object>[] Queues;                // id is queueId

        // helpers
        private Dictionary<string, int> WorkerNameToId;
        private Dictionary<string, int> NodeNameToId;
        private Dictionary<string, int> QueueNameToId;
        private bool[] QueueIsToEl;                                 // id is queueId
        private bool[] QueueIsFromEl;                               // id is queueId

        private log4net.ILog Logger;                                // logger from external code

        private bool IsLoadDiagonstics;

        private bool Disposed;


        
        public ForemanBase()
        {
            Config = new ForemanConfigurationFile();
        }

        public ForemanBase(string PathToSettingsFile)
        {
            this.PathToSettingsFile = PathToSettingsFile;
            Config = new ForemanConfigurationFile();
        }

        public ForemanBase(string PathToSettingsFile, log4net.ILog Logger)
        {
            this.PathToSettingsFile = PathToSettingsFile;
            this.Logger = Logger;
            Config = new ForemanConfigurationFile();
        }

        public void Load(string PathToSettingsFile)
        {
            this.PathToSettingsFile = PathToSettingsFile;
            Load();
        }

        public void Load()
        {
            if (Disposed)
                return;

            if (PathToSettingsFile == null)
                throw new ArgumentNullException("PathToSettingsFile");

            if (!File.Exists(PathToSettingsFile))
                throw new FileNotFoundException(PathToSettingsFile);

            Logger?.Info("Foreman LoadSettingsFile() start");

            int WorkerCounter = 0;
            int NodeCounter = 0;
            int QueueCounter = 0;

            WorkerActivator = new WorkerActivator();
            
            try
            {
                string settings = File.ReadAllText(PathToSettingsFile);
                Config = JsonConvert.DeserializeObject<ForemanConfigurationFile>(settings);
            }
            catch (Exception ex)
            {
                string err = "Can't parse config file: " + PathToSettingsFile + "(" + ex.Message + ")";
                Logger?.Error(err);
                throw new Exception(err, ex);
            }

            if (Config.foremanId.Length == 0)
                throw new ArgumentException("foremanId must not be empty string");

            Id = Config.foremanId;

            // Register workers
            Logger?.Info("Registering workers");
            if (Config.workers == null || Config.workers.Count == 0)
                throw new ArgumentException("No workers in config file");

            WorkerNameToId = new Dictionary<string, int>(Config.workers.Count);
            foreach (var configWorker in Config.workers)
            {
                if (WorkerNameToId.ContainsKey(configWorker.name))
                {
                    string err = "The worker name '" + configWorker.name + "' is already registered";
                    Logger?.Error(err);
                    throw new ArgumentException(err);
                }

                int workerId = WorkerCounter;
                WorkerCounter++;

                try
                {
                    WorkerActivator.RegisterWorkerType(workerId, configWorker.className);
                    WorkerNameToId.Add(configWorker.name, workerId);
                }
                catch (Exception ex)
                {
                    string err = "Can't create a worker using className (" + ex.Message + ")";
                    Logger?.Error(err);
                    throw new ArgumentException(err, ex);
                }
            }

            // Register nodes
            Logger?.Info("Registering nodes");
            if (Config.nodes == null || Config.nodes.Count == 0)
            {
                string err = "No nodes in config file";
                Logger?.Error(err);
                throw new ArgumentException(err);
            }

            Nodes = new WorkerNode[Config.nodes.Count];
            NodeState = new WorkerNodeState[Config.nodes.Count];
            WorkerNodeOrder = new Dictionary<int, List<WorkerNode>>();
            NodeNameToId = new Dictionary<string, int>(Config.nodes.Count);
            foreach (var configNode in Config.nodes)
            {
                string workerName = configNode.worker;
                if (!WorkerNameToId.ContainsKey(workerName))
                {
                    string err = "The worker name '" + workerName + "' in nodes section is not defined in workers section";
                    Logger?.Error(err);
                    throw new ArgumentException(err);
                }
                    
                int workerId = WorkerNameToId[workerName];
                
                if (NodeNameToId.ContainsKey(configNode.name))
                {
                    string err = "The node name '" + configNode.name + "' is already registered";
                    Logger?.Error(err);
                    throw new ArgumentException(err);
                }

                var node = new WorkerNode();
                node.Id = NodeCounter;
                node.OrderId = configNode.orderId;
                node.IsWaitToFinish = configNode.isWaitToFinish;
                node.Name = configNode.name;
                node.WorkerId = workerId;
                node.WorkerActivator = WorkerActivator;

                if (!WorkerNodeOrder.ContainsKey(node.OrderId))
                    WorkerNodeOrder.Add(node.OrderId, new List<WorkerNode>() { node });
                else
                    WorkerNodeOrder[node.OrderId].Add(node);

                Nodes[NodeCounter] = node;
                NodeState[NodeCounter] = WorkerNodeState.Idle;
                NodeNameToId.Add(node.Name, node.Id);
                
                NodeCounter++;
            }

            // Register queues
            Logger?.Info("Registering queues");
            if (Config.queues == null || Config.queues.Count == 0)
            {
                Logger?.Info("No queues in config file");
            } 
            else
            {
                Queues = new BlockingCollection<object>[Config.queues.Count];
                QueueNameToId = new Dictionary<string, int>(Config.queues.Count);
                QueueIsToEl = new bool[Config.queues.Count];
                QueueIsFromEl = new bool[Config.queues.Count];
                foreach (var configQueue in Config.queues)
                {
                    if (QueueNameToId.ContainsKey(configQueue.name))
                    {
                        string err = "The queue name '" + configQueue.name + "' is already registered";
                        Logger?.Error(err);
                        throw new ArgumentException(err);
                    }

                    int queueId = QueueCounter;

                    if (configQueue.bufferLimit == 0)
                        Queues[queueId] = new BlockingCollection<object>();
                    else
                        Queues[queueId] = new BlockingCollection<object>(configQueue.bufferLimit);

                    QueueNameToId.Add(configQueue.name, queueId);

                    QueueCounter++;
                }
            }

            // Register connections
            Logger?.Info("Building topology");
            foreach (var configConnection in Config.connections)
            {
                string fromName = configConnection.from;
                string toName = configConnection.to;

                int fromElId, toElId;

                TopologyElementType fromEl = GetNameTopologyType(fromName, out fromElId);
                TopologyElementType toEl = GetNameTopologyType(toName, out toElId);

                if (fromEl == TopologyElementType.None && toEl == TopologyElementType.None)
                {
                    string err = "Connection from and to elements do not exist: '" + fromName + "' -> '" + toName + "'";
                    Logger?.Error(err);
                    throw new Exception(err);
                }

                // node to node, node to queue and queue to node are supported
                // queue to queue is not supported
                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Queue)
                {
                    string err = "Can't connect a queue to a queue: '" + fromName + "' -> '" + toName + "'";
                    Logger?.Error(err);
                    throw new Exception(err);
                }

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Queue)
                {
                    var node = Nodes[fromElId];
                    var queue = Queues[toElId];

                    if (node.Output != null)
                    {
                        string err = "Can't set two output elements for same node: '" + fromName + "' -> '" + toName + "'";
                        Logger?.Error(err);
                        throw new Exception();
                    }
                    
                    node.Output = queue;
                    node.IsConnected = true;
                    QueueIsToEl[toElId] = true;
                }

                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Node)
                {
                    var node = Nodes[toElId];
                    var queue = Queues[fromElId];

                    if (node.Input != null)
                    {
                        string err = "Can't set two input elements for the same node: '" + fromName + "' -> '" + toName + "'";
                        Logger?.Error(err);
                        throw new Exception(err);
                    }

                    node.Input = queue;
                    node.IsConnected = true;
                    QueueIsFromEl[fromElId] = true;
                }

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Node)
                {
                    var node1 = Nodes[fromElId];
                    node1.IsConnected = true;

                    var node2 = Nodes[toElId];
                    node2.IsConnected = true;

                    if (node1.IsWaitToFinish && node2.IsWaitToFinish)
                        node1.NextNode = node2;
                }
            }

            DryRun();

            // dispose of helpers
            WorkerNameToId = null;
            NodeNameToId = null;
            QueueNameToId = null;

            Logger?.Info("Foreman LoadSettingsFile() finish");
        }

        public void DryRun()
        {
            // verify connections

            // iterate over all tree and check if there are any unconnected nodes
            foreach (var node in Nodes)
                if (!node.IsConnected)
                {
                    string err = "Node is not connected to topology tree: '" + node.Name + "'";
                    Logger?.Error(err);
                    throw new Exception(err);
                }

            if (Queues != null)
            {
                // check a queue is not an edge
                for (var i = 0; i < Queues.Length; i++)
                {
                    if (!QueueIsFromEl[i] || !QueueIsToEl[i])
                    {
                        string err = "A queue cannot be an edge, but must connect a node as input and another node as output";
                        Logger?.Error(err);
                        throw new Exception(err);
                    }
                }
            }

            // several independent topologies can coexist in a single foreman

            // dispose of helpers
            QueueIsFromEl = null;
            QueueIsToEl = null;
        }

        public void Run()
        {
            Logger?.Info("Foreman Run() start");

            var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            List<Task> orderedNonWaitingNodeTasks = new List<Task>();
            List<WorkerNode> orderedWaitingNodes = new List<WorkerNode>();

            var orderedNodes = WorkerNodeOrder.Keys.OrderBy(x =>  x);

            foreach (var orderId in orderedNodes)
            {
                var nodes = WorkerNodeOrder[orderId];

                foreach (var node in nodes)
                {
                    if (node.IsWaitToFinish)
                        orderedWaitingNodes.Add(node);
                    else
                        orderedNonWaitingNodeTasks.Add(f.StartNew(() =>
                        {
                            OnWorkerNodeStarted(node.Id);

                            try
                            {
                                node.Run();
                            }
                            catch (Exception ex)
                            {
                                OnWorkerNodeError(node.Id, ex);
                            }

                            OnWorkerNodeEnded(node.Id);
                        }));
                }
                
            }

            // execute waiting nodes synchronously
            for (var i=0; i<orderedWaitingNodes.Count; i++)
            {
                var node = orderedWaitingNodes[i];

                OnWorkerNodeStarted(node.Id);

                try
                {
                    node.Run();
                }
                catch (Exception ex)
                {
                    OnWorkerNodeError(node.Id, ex);
                    Console.WriteLine("Error");
                    IsLoadDiagonstics = true;
                }

                OnWorkerNodeEnded(node.Id);
            }

            // wait on non waiting nodes to finish
            Task.WaitAll(orderedNonWaitingNodeTasks.ToArray());

            Logger?.Info("Foreman Run() finish");
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void Terminate(bool IsGraceful = true) { }

        private TopologyElementType GetNameTopologyType(string Name, out int id)
        {
            int nodeId = 0, queueId = 0, workerId = 0;
            bool isNode, isQueue;

            if (NodeNameToId == null)
                isNode = false;
            else
                isNode = NodeNameToId.TryGetValue(Name, out nodeId);

            if (QueueNameToId == null)
                isQueue = false;
            else
                isQueue = QueueNameToId.TryGetValue(Name, out queueId);

            if (isNode && isQueue)
            {
                string err = "The name '" + Name + "' is ambigious - a node or a queue?";
                Logger?.Error(err);
                throw new Exception(err);
            }

            bool isWorker = WorkerNameToId.TryGetValue(Name, out workerId);

            if ((isNode && isWorker) || (isQueue && isWorker))
            {
                string err = "The name '" + Name + "' is ambigious";
                Logger?.Error(err);
                throw new Exception(err);
            }

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

        #region WorkerNode Events

        public void OnWorkerNodeStarted(int NodeId)
        {
            NodeState[NodeId] = WorkerNodeState.Running;
            //Console.WriteLine(NodeId.ToString() + " started");
        }

        public void OnWorkerNodeEnded(int NodeId)
        {
            NodeState[NodeId] = WorkerNodeState.Finished;
            //Console.WriteLine(NodeId.ToString() + " finished");
        }

        public void OnWorkerNodeError(int NodeId, Exception ex)
        {
            NodeState[NodeId] = WorkerNodeState.Error;
            //Console.WriteLine(NodeId.ToString() + " error");
        }

        #endregion
    }
}
