using Batch.Worker;
using BatchFoundation.Worker;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    public abstract class ForemanBase : IDisposable
    {
        public string Id;
        public bool IsNodesLongRunning;
        public string PathToConfigFile;

        private ForemanConfigurationFile Config;

        private WorkerNode[] Nodes;                                     // id is nodeId
        private WorkerNodeState[] NodeState;                            // id is nodeId
        private Dictionary<int, List<WorkerNode>> WorkerNodeExeOrder;   // id is orderId
        private BlockingCollection<object>[] Queues;                    // id is queueId
        private Assembly asm;

        // helpers
        private Dictionary<string, int> NodeNameToId;
        private Dictionary<string, int> QueueNameToId;
        private bool[] QueueIsToEl;                                     // id is queueId
        private bool[] QueueIsFromEl;                                   // id is queueId

        private bool Disposed;



        public ForemanBase()
        {

        }

        public ForemanBase(string PathToConfigFile)
        {
            this.PathToConfigFile = PathToConfigFile;
        }

        internal ForemanBase(ForemanConfigurationFile Config)
        {
            this.Config = Config;
        }

        public void Load()
        {
            if (Disposed)
                return;

            if (PathToConfigFile == null && Config == null)
                throw new ArgumentNullException("PathToConfigFile");

            // load config file only if not already defined by Contractor
            if (Config == null)
                Config = LoadConfigFile(PathToConfigFile);
            
            int NodeCounter = 0;
            int QueueCounter = 0;

            Id = Config.foremanId;
            IsNodesLongRunning = Config.isNodesLongRunning;

            /*
            // register assemblies
            foreach (var configAssembly in Config.assemblies)
                WorkerLoader.RegisterInstance(configAssembly.name, configAssembly.path);
            */

            // register assembly
            asm = Assembly.LoadFile(Config.assemblyPath);

            // Register nodes
            if (Config.nodes == null || Config.nodes.Count == 0)
            {
                string err = "No nodes in config file";
                throw new ArgumentException(err);
            }

            Nodes = new WorkerNode[Config.nodes.Count];
            NodeState = new WorkerNodeState[Config.nodes.Count];
            WorkerNodeExeOrder = new Dictionary<int, List<WorkerNode>>();
            NodeNameToId = new Dictionary<string, int>(Config.nodes.Count);
            foreach (var configNode in Config.nodes)
            {
                var node = new WorkerNode();
                node.Id = NodeCounter;
                node.OrderId = configNode.exeOrderId;
                node.Name = configNode.name;
                node.WorkerClassName = configNode.className;

                if (IsNodesLongRunning)
                    node.IsLongRunning = true;

                /*
                WorkerLoader wl;
                if (!WorkerLoader.TryGetInstanceByAppDomainName(configNode.assemblyName, out wl))
                    throw new Exception("Assembly not found for node: " + configNode.assemblyName);

                node.WorkerLoader = wl;
                */

                var workerType = asm.GetTypes().First(x => x.FullName.Equals(configNode.className));
                var worker = (WorkerBase)Activator.CreateInstance(workerType);

                if (worker == null)
                    throw new Exception("Can't create instance from" + configNode.className);

                node.Worker = worker;

                if (!WorkerNodeExeOrder.ContainsKey(node.OrderId))
                    WorkerNodeExeOrder.Add(node.OrderId, new List<WorkerNode>() { node });
                else
                    WorkerNodeExeOrder[node.OrderId].Add(node);

                Nodes[NodeCounter] = node;
                NodeState[NodeCounter] = WorkerNodeState.Idle;
                NodeNameToId.Add(node.Name, node.Id);
                
                NodeCounter++;
            }

            // Register queues
            if (Config.queues == null || Config.queues.Count == 0)
            {
                // no queues
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
                    throw new Exception(err);
                }

                // node to node, node to queue and queue to node are supported
                // queue to queue is not supported
                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Queue)
                {
                    string err = "Can't connect a queue to a queue: '" + fromName + "' -> '" + toName + "'";
                    throw new Exception(err);
                }

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Queue)
                {
                    var node = Nodes[fromElId];
                    var queue = Queues[toElId];

                    if (node.Output != null)
                    {
                        string err = "Can't set two output elements for same node: '" + fromName + "' -> '" + toName + "'";
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

                    if (!node1.IsLongRunning && !node2.IsLongRunning)
                        node1.NextNode = node2;
                }
            }

            // dry run

            // verify connections

            // iterate over all tree and check if there are any unconnected nodes
            foreach (var node in Nodes)
                if (!node.IsConnected)
                {
                    string err = "Node is not connected to topology tree: '" + node.Name + "'";
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
                        throw new Exception(err);
                    }
                }
            }

            // several independent topologies can coexist in a single foreman

            // dispose of helpers
            QueueIsFromEl = null;
            QueueIsToEl = null;
            NodeNameToId = null;
            QueueNameToId = null;
        }

        public ForemanConfigurationFile LoadConfigFile(string PathToConfigFile)
        {
            ForemanConfigurationFile Config;

            try
            {
                string settings = File.ReadAllText(PathToConfigFile);
                Config = JsonConvert.DeserializeObject<ForemanConfigurationFile>(settings);
            }
            catch (Exception ex)
            {
                string err = "Can't parse config file: " + PathToConfigFile + "(" + ex.Message + ")";
                throw new Exception(err, ex);
            }

            if (Config.foremanId.Length == 0)
                throw new ArgumentException("foremanId must not be empty string");

            return Config;
        }

        public void Run()
        {
            var orderedNodes = WorkerNodeExeOrder.Keys.OrderBy(x => x);

            if (IsNodesLongRunning)
            {
                var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                List<Task> orderedNonWaitingNodeTasks = new List<Task>();

                foreach (var orderId in orderedNodes)
                {
                    var nodes = WorkerNodeExeOrder[orderId];

                    foreach (var node in nodes)
                    {
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
            }
            else
            {
                foreach (var orderId in orderedNodes)
                {
                    var nodes = WorkerNodeExeOrder[orderId];

                    foreach (var node in nodes)
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
                    }

                }
            }
            

            /*
            // Foreman can have both long running and short running tasks - not applicable, so overriden in child classes

            var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            List<Task> orderedNonWaitingNodeTasks = new List<Task>();
            List<WorkerNode> orderedWaitingNodes = new List<WorkerNode>();

            var orderedNodes = WorkerNodeExeOrder.Keys.OrderBy(x =>  x);

            foreach (var orderId in orderedNodes)
            {
                var nodes = WorkerNodeExeOrder[orderId];

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
                }

                OnWorkerNodeEnded(node.Id);
            }

            // wait on non waiting nodes to finish
            Task.WaitAll(orderedNonWaitingNodeTasks.ToArray());

            */
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void Terminate(bool IsGraceful = true)
        {
            if (Disposed)
                return;
        }

        private TopologyElementType GetNameTopologyType(string Name, out int id)
        {
            int nodeId = 0, queueId = 0;
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
            Console.WriteLine("Node " + NodeId.ToString() + " exception: " + ex.Message);
        }

        #endregion
    }
}
