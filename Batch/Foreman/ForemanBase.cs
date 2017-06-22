using Batch.Contractor;
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
using System.Threading;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal abstract class ForemanBase : IForeman, IDisposable
    {
        public string Id
        {
            get;
            set;
        }
        public bool IsNodesLongRunning
        {
            get;
            private set;
        }
        public string ConfigString
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
        public bool IsError
        {
            get;
            private set;
        }

        // short running foreman to short running foreman connection
        public IForeman NextForeman
        {
            get;
            set;
        }
        public IForeman TestForeman
        {
            get;
            set;
        }
        public int TestForemanRequestWeight
        {
            get;
            set;
        }

        public IEnumerable<WorkerNodeState> WorkerNodeStates
        {
            get
            {
                foreach (var node in nodes)
                    yield return node.State;
            }
            /*
            get
            {
                return nodes.Select(x => x.State);
            }
            */
        }
        public Exception WorkerNodeException
        {
            get;
            private set;
        }
        public string WorkerNodeExceptionString
        {
            get;
            private set;
        }

        public ForemanConfigurationFile Config
        {
            get;
            set;
        }

        public ContractorSettings ContractorSettings
        {
            get;
            set;
        }

        private WorkerNode[] nodes;                                         // id is nodeId
        private Dictionary<int, List<WorkerNode>> workerNodeExeOrder;       // key is orderId
        private BlockingCollection<object>[] queues;                        // id is queueId
        private Dictionary<string, int> queueNameToId;
        private Assembly asm;
        private object emptyObj = new { };

        // helpers
        private Dictionary<string, int> nodeNameToId;
        //private bool[] queueIsToEl;                                       // id is queueId
        //private bool[] queueIsFromEl;                                     // id is queueId

        private List<Task> orderedLongRunningNodeTasks;

        private bool IsDisposed;
        


        public ForemanBase(string ConfigString)
        {
            this.ConfigString = ConfigString;
            IsRunning = false;
            IsPaused = false;
            IsLoaded = false;
            IsRanAtLeastOnce = false;
        }

        public ForemanBase(ForemanConfigurationFile Config)
        {
            this.Config = Config;
            IsRunning = false;
            IsPaused = false;
            IsLoaded = false;
            IsRanAtLeastOnce = false;
        }

        public void Load()
        {
            if (IsDisposed)
                return;

            if (IsLoaded)
                throw new Exception("Foreman already loaded");

            if (IsRunning)
                return;

            if (ConfigString == null && Config == null)
                throw new ArgumentNullException("ConfigString");

            if (ContractorSettings == null)
                ContractorSettings = new ContractorSettings();

            // load config file only if not already defined by Contractor
            if (Config == null)
                Config = ParseConfigString(ConfigString);
            
            int NodeCounter = 0;
            int QueueCounter = 0;

            IsNodesLongRunning = Config.isNodesLongRunning;

            // register assembly
            if (Config.assemblyPath == null || Config.assemblyPath.Length == 0)
                throw new Exception("assemblyPath field in Foreman configuration file cannot be empty");

            if (!Path.IsPathRooted(Config.assemblyPath))
            {
                if (ContractorSettings.ForemanDllBaseDir == null)
                    throw new Exception("if the assemblyPath field is a relative path then ContractorSettings.ForemanDllBaseDir must be defined");

                Config.assemblyPath = Path.Combine(ContractorSettings.ForemanDllBaseDir, Config.assemblyPath);
            }

            asm = Assembly.LoadFile(Config.assemblyPath);

            // Register nodes
            if (Config.nodes == null || Config.nodes.Count == 0)
                throw new ArgumentException("No nodes in config file");

            nodes = new WorkerNode[Config.nodes.Count];
            workerNodeExeOrder = new Dictionary<int, List<WorkerNode>>();
            nodeNameToId = new Dictionary<string, int>(Config.nodes.Count);
            foreach (var configNode in Config.nodes)
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

                node.State = WorkerNodeState.Idle;
                nodes[NodeCounter] = node;
                nodeNameToId.Add(node.Name, node.Id);
                
                NodeCounter++;
            }

            // Register queues
            if (Config.queues != null && Config.queues.Count > 0)
            {
                if (!IsNodesLongRunning)
                    throw new Exception("Can't define queues in short running foremen");

                queues = new BlockingCollection<object>[Config.queues.Count];
                queueNameToId = new Dictionary<string, int>(Config.queues.Count);
                //queueIsToEl = new bool[config.queues.Count];
                //queueIsFromEl = new bool[config.queues.Count];
                foreach (var configQueue in Config.queues)
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
            foreach (var configConnection in Config.connections)
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
                    //queueIsToEl[toElId] = true;
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
                    //queueIsFromEl[fromElId] = true;
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

            // a single node may not be connected
            if (nodes.Length == 1)
                nodes[0].IsConnected = true;

            // iterate over all tree and check if there are any unconnected nodes
            foreach (var node in nodes)
                if (!node.IsConnected)
                {
                    string err = "Node is not connected to topology tree: '" + node.Name + "'";
                    throw new Exception(err);
                }

            /*
            
            // queues CAN be an edge
            
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
            */

            // several independent topologies can coexist in a single foreman

            // dispose of helpers
            //queueIsFromEl = null;
            //queueIsToEl = null;
            nodeNameToId = null;

            IsLoaded = true;
        }

        public void Run()
        {
            Run(ref emptyObj);
        }

        public void Run(ref object Data, bool IsTestForeman = false)
        {
            // if IsNodesLongRunning is true then Run() is expected to run once until Dispose() is executed
            // if IsNodesLongRunning is false then Run() is expected run again and again

            if (IsDisposed)
                return;

            if (!IsLoaded)
                throw new Exception("Foreman not loaded yet");

            if (IsPaused)
                throw new Exception("Foreman is stopped");

            if (IsRunning)
                throw new Exception("Foreman is already running");

            if (IsNodesLongRunning && IsRanAtLeastOnce)
                throw new Exception("Long running foremen cannot be run more than once");

            IsRunning = true;
            IsRanAtLeastOnce = true;

            var orderedNodes = workerNodeExeOrder.Keys.OrderBy(x => x);

            if (IsNodesLongRunning)
            {
                var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                orderedLongRunningNodeTasks = new List<Task>();

                foreach (var orderId in orderedNodes)
                {
                    var nodes = workerNodeExeOrder[orderId];

                    foreach (var node in nodes)
                    {
                        orderedLongRunningNodeTasks.Add(f.StartNew(() => {
                            OnWorkerNodeStarted(node.Id);

                            try
                            {
                                node.Run(ref emptyObj);
                            }
                            catch (Exception ex)
                            {
                                OnWorkerNodeError(node.Id, ex);
                                return;
                            }

                            OnWorkerNodeEnded(node.Id);
                        }));
                    }

                    // wait on non waiting nodes to finish
                    //Task.WaitAll(orderedNonWaitingNodeTasks.ToArray());

                    // IsRunning will never be false again in a long running foreman
                    //IsRunning = false;

                    // don't wait on tasks so this thread is not blocked and IsRunning is always true until foreman is disposed
                }
            }
            else
            {
                bool isFirstNode = true;

                foreach (var orderId in orderedNodes)
                {
                    var nodes = workerNodeExeOrder[orderId];

                    if (isFirstNode && nodes.Count > 0)
                    {
                        // put external data into first node (from foreman upstream?)
                        nodes[0].Data = Data;
                        isFirstNode = false;
                    }

                    foreach (var node in nodes)
                    {
                        OnWorkerNodeStarted(node.Id);

                        try
                        {
                            node.Run(ref Data, IsTestForeman);
                        }
                        catch (Exception ex)
                        {
                            OnWorkerNodeError(node.Id, ex);
                            return;
                        }

                        OnWorkerNodeEnded(node.Id);
                    }
                }
                
                IsRunning = false;
            }
        }

        public void Pause()
        {
            if (IsDisposed)
                return;

            if (IsPaused)
                return;

            if (IsNodesLongRunning)
                throw new Exception("Long running foremen can't be paused");
            else
                IsPaused = true;

            // if Run() is already run it is not interrupted, but it won't run again until
            // foreman is resumed
        }

        public void Resume()
        {
            if (IsDisposed)
                return;

            if (!IsPaused)
                return;

            if (IsNodesLongRunning)
                throw new Exception("Long running foremen can't be resumed");
            else
                IsPaused = false;
        }

        public bool SubmitData(string QueueName, object data)
        {
            if (IsDisposed)
                return false;

            if (!IsNodesLongRunning)
                throw new Exception("SubmitData() is used only in long running foremen");

            if (QueueName == null)
                throw new ArgumentNullException("QueueName");

            if (data == null)
                throw new ArgumentNullException("data");

            int qId;
            if (!queueNameToId.TryGetValue(QueueName, out qId))
                throw new Exception("Queue doesn't exist");

            try
            {
                queues[qId].Add(data);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool CompleteAdding(string QueueName)
        {
            if (IsDisposed)
                return false;

            if (!IsNodesLongRunning)
                throw new Exception("CompleteAdding() is used only in long running foremen");

            if (QueueName == null)
                throw new ArgumentNullException("QueueName");

            int qId;
            if (!queueNameToId.TryGetValue(QueueName, out qId))
                throw new Exception("Queue doesn't exist");

            var q = queues[qId];

            try
            {
                q.CompleteAdding();
                return q.IsAddingCompleted;
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        public string ExportToConfigString()
        {
            string str = null;

            try
            {
                str = JsonConvert.SerializeObject(Config);
            }
            catch (Exception ex)
            {
                string err = "Can't serialize config string (" + ex.Message + ")";
                throw new Exception(err, ex);
            }

            return str;
        }

        public Tuple<long, long, long, TimeSpan> GetAppDomainMonitoringData()
        {
            return null;
        }

        public void OnWorkerNodeStarted(int NodeId)
        {
            nodes[NodeId].State = WorkerNodeState.Running;
            //Console.WriteLine("Node " + NodeId.ToString() + " started");
        }

        public void OnWorkerNodeEnded(int NodeId)
        {
            nodes[NodeId].State = WorkerNodeState.Done;
            //Console.WriteLine("Node " + NodeId.ToString() + " finished");
        }

        public void OnWorkerNodeError(int NodeId, Exception ex)
        {
            nodes[NodeId].State = WorkerNodeState.Error;
            nodes[NodeId].Exception = ex;
            WorkerNodeException = ex;
            WorkerNodeExceptionString = nodes[NodeId].Name + " error: " + ex.Message;
            IsError = true;
            IsRunning = false;
            //Console.WriteLine("Node " + NodeId.ToString() + " exception: " + ex.Message);
        }

        public void Dispose()
        {
            IsDisposed = true;
            IsPaused = true;

            // should do this in an orderly fashion to avoid exceptions?
            if (queues != null)
                foreach (var q in queues)
                    q.CompleteAdding();

            // terminate tasks

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

        private ForemanConfigurationFile ParseConfigString(string ConfigString)
        {
            ForemanConfigurationFile Config;

            try
            {
                Config = JsonConvert.DeserializeObject<ForemanConfigurationFile>(ConfigString);
            }
            catch (Exception ex)
            {
                string err = "Can't parse config string (" + ex.Message + ")";
                throw new Exception(err, ex);
            }

            return Config;
        }
    }
}
