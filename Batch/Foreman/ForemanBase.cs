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
    internal abstract class ForemanBase : IForeman, IDisposable
    {
        public string Id
        {
            get;
            private set;
        }
        public bool IsNodesLongRunning
        {
            get;
            private set;
        }
        public string PathToConfigFile
        {
            get;
            private set;
        }
        public bool IsLoaded
        {
            get;
            private set;
        }
        public bool IsRunning
        {
            get;
            private set;
        }
        public bool IsPaused
        {
            get;
            private set;
        }
        public bool IsRanAtLeastOnce
        {
            get;
            private set;
        }

        private ForemanConfigurationFile config;
        private object data;

        private WorkerNode[] nodes;                                     // id is nodeId
        private WorkerNodeState[] nodeState;                            // id is nodeId
        private Dictionary<int, List<WorkerNode>> workerNodeExeOrder;   // id is orderId
        private BlockingCollection<object>[] queues;                    // id is queueId
        private Assembly asm;

        // helpers
        private Dictionary<string, int> nodeNameToId;
        private Dictionary<string, int> queueNameToId;
        private bool[] queueIsToEl;                                     // id is queueId
        private bool[] queueIsFromEl;                                   // id is queueId

        private bool Disposed;
        


        public ForemanBase(string PathToConfigFile)
        {
            this.PathToConfigFile = PathToConfigFile;
            IsRunning = false;
            IsPaused = false;
            IsLoaded = false;
            IsRanAtLeastOnce = false;
        }

        public void Load()
        {
            if (Disposed)
                return;

            if (IsRunning)
                return;

            if (PathToConfigFile == null && config == null)
                throw new ArgumentNullException("PathToConfigFile");

            // load config file only if not already defined by Contractor
            if (config == null)
                config = LoadConfigFile(PathToConfigFile);
            
            int NodeCounter = 0;
            int QueueCounter = 0;

            Id = config.foremanId;
            IsNodesLongRunning = config.isNodesLongRunning;

            /*
            // register assemblies
            foreach (var configAssembly in Config.assemblies)
                WorkerLoader.RegisterInstance(configAssembly.name, configAssembly.path);
            */

            // register assembly
            if (config.assemblyPath == null || config.assemblyPath.Length == 0)
                throw new Exception("assemblyPath field in Foreman configuration file cannot be empty");

            asm = Assembly.LoadFile(config.assemblyPath);

            // Register nodes
            if (config.nodes == null || config.nodes.Count == 0)
                throw new ArgumentException("No nodes in config file");

            nodes = new WorkerNode[config.nodes.Count];
            nodeState = new WorkerNodeState[config.nodes.Count];
            workerNodeExeOrder = new Dictionary<int, List<WorkerNode>>();
            nodeNameToId = new Dictionary<string, int>(config.nodes.Count);
            foreach (var configNode in config.nodes)
            {
                var node = new WorkerNode();
                node.Id = NodeCounter;
                node.OrderId = configNode.exeOrderId;
                node.Name = configNode.name;
                node.WorkerClassName = configNode.className;

                var workerType = asm.GetTypes().First(x => x.FullName.Equals(configNode.className));

                if (workerType == null)
                    throw new Exception("Type not found: " + configNode.className);

                var worker = (WorkerBase)Activator.CreateInstance(workerType);

                if (worker == null)
                    throw new Exception("Can't create instance from" + configNode.className);

                node.Worker = worker;

                if (!workerNodeExeOrder.ContainsKey(node.OrderId))
                    workerNodeExeOrder.Add(node.OrderId, new List<WorkerNode>() { node });
                else
                    workerNodeExeOrder[node.OrderId].Add(node);

                nodes[NodeCounter] = node;
                nodeState[NodeCounter] = WorkerNodeState.Idle;
                nodeNameToId.Add(node.Name, node.Id);
                
                NodeCounter++;
            }

            // Register queues
            if (config.queues == null || config.queues.Count == 0)
            {
                // no queues
            } 
            else
            {
                queues = new BlockingCollection<object>[config.queues.Count];
                queueNameToId = new Dictionary<string, int>(config.queues.Count);
                queueIsToEl = new bool[config.queues.Count];
                queueIsFromEl = new bool[config.queues.Count];
                foreach (var configQueue in config.queues)
                {
                    if (queueNameToId.ContainsKey(configQueue.name))
                    {
                        string err = "The queue name '" + configQueue.name + "' is already registered";
                        throw new ArgumentException(err);
                    }

                    int queueId = QueueCounter;

                    if (configQueue.bufferLimit == 0)
                        queues[queueId] = new BlockingCollection<object>();
                    else
                        queues[queueId] = new BlockingCollection<object>(configQueue.bufferLimit);

                    queueNameToId.Add(configQueue.name, queueId);

                    QueueCounter++;
                }
            }

            // Register connections
            foreach (var configConnection in config.connections)
            {
                string fromName = configConnection.from;
                string toName = configConnection.to;

                int fromElId, toElId;

                TopologyElementType fromEl = GetTopologyTypeByName(fromName, out fromElId);
                TopologyElementType toEl = GetTopologyTypeByName(toName, out toElId);

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
                    var node = nodes[fromElId];
                    var queue = queues[toElId];

                    if (node.Output != null)
                    {
                        string err = "Can't set two output elements for same node: '" + fromName + "' -> '" + toName + "'";
                        throw new Exception();
                    }
                    
                    node.Output = queue;
                    node.IsConnected = true;
                    queueIsToEl[toElId] = true;
                }

                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Node)
                {
                    var node = nodes[toElId];
                    var queue = queues[fromElId];

                    if (node.Input != null)
                    {
                        string err = "Can't set two input elements for the same node: '" + fromName + "' -> '" + toName + "'";
                        throw new Exception(err);
                    }

                    node.Input = queue;
                    node.IsConnected = true;
                    queueIsFromEl[fromElId] = true;
                }

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Node)
                {
                    var node1 = nodes[fromElId];
                    node1.IsConnected = true;

                    var node2 = nodes[toElId];
                    node2.IsConnected = true;

                    if (!IsNodesLongRunning)
                        node1.NextNode = node2;
                }
            }

            // dry run

            // verify connections

            // iterate over all tree and check if there are any unconnected nodes
            foreach (var node in nodes)
                if (!node.IsConnected)
                {
                    string err = "Node is not connected to topology tree: '" + node.Name + "'";
                    throw new Exception(err);
                }

            if (queues != null)
            {
                // check a queue is not an edge
                for (var i = 0; i < queues.Length; i++)
                {
                    if (!queueIsFromEl[i] || !queueIsToEl[i])
                    {
                        string err = "A queue cannot be an edge, but must connect a node as input and another node as output";
                        throw new Exception(err);
                    }
                }
            }

            // several independent topologies can coexist in a single foreman

            // dispose of helpers
            queueIsFromEl = null;
            queueIsToEl = null;
            nodeNameToId = null;
            queueNameToId = null;

            IsLoaded = true;
        }

        public void Run()
        {
            // if IsNodesLongRunning is true then Run() is expected to run once until Dispose() is executed
            // if IsNodesLongRunning is false then Run() is expected run again and again

            if (Disposed)
                return;

            if (!IsLoaded)
                throw new Exception("Foreman not loaded yet");

            if (IsPaused)
                throw new Exception("Foreman is paused");

            if (IsRunning)
                throw new Exception("Foreman is already running");

            if (IsNodesLongRunning && IsRanAtLeastOnce)
                throw new Exception("long running foremen (IsNodesLongRunning = true) cannot be run more than once");

            IsRunning = true;
            IsRanAtLeastOnce = true;

            var orderedNodes = workerNodeExeOrder.Keys.OrderBy(x => x);

            if (IsNodesLongRunning)
            {
                var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                List<Task> orderedNonWaitingNodeTasks = new List<Task>();

                foreach (var orderId in orderedNodes)
                {
                    var nodes = workerNodeExeOrder[orderId];

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
                    var nodes = workerNodeExeOrder[orderId];

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

            IsRunning = false;

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

        public void Pause()
        {
            if (IsNodesLongRunning)
                throw new Exception("A long running Foreman (IsNodesLongRunning = true) can't be paused");

            // if Run() is already run it is not interrupted, but it won't run again until
            // foreman is resumed

            IsPaused = true;
        }

        public void Resume()
        {
            if (IsNodesLongRunning)
                throw new Exception("A long running Foreman (IsNodesLongRunning = true) can't be resumed");

            IsPaused = false;
        }

        public object GetData()
        {
            return data;
        }

        public void SetData(object data)
        {
            this.data = data;
        }

        public void Dispose()
        {
            Disposed = true;

            if (queues != null)
                foreach (var q in queues)
                    q.CompleteAdding();

            queues = null;
            asm = null;
            workerNodeExeOrder = null;
            nodeNameToId = null;
            queueNameToId = null;
        }

        private TopologyElementType GetTopologyTypeByName(string Name, out int id)
        {
            int nodeId = 0, queueId = 0;
            bool isNode, isQueue;

            if (nodeNameToId == null)
                isNode = false;
            else
                isNode = nodeNameToId.TryGetValue(Name, out nodeId);

            if (queueNameToId == null)
                isQueue = false;
            else
                isQueue = queueNameToId.TryGetValue(Name, out queueId);

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

        private ForemanConfigurationFile LoadConfigFile(string PathToConfigFile)
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

        public void OnWorkerNodeStarted(int NodeId)
        {
            nodeState[NodeId] = WorkerNodeState.Running;
            //Console.WriteLine(NodeId.ToString() + " started");
        }

        public void OnWorkerNodeEnded(int NodeId)
        {
            nodeState[NodeId] = WorkerNodeState.Finished;
            //Console.WriteLine(NodeId.ToString() + " finished");
        }

        public void OnWorkerNodeError(int NodeId, Exception ex)
        {
            nodeState[NodeId] = WorkerNodeState.Error;
            Console.WriteLine("Node " + NodeId.ToString() + " exception: " + ex.Message);
        }

    }
}
