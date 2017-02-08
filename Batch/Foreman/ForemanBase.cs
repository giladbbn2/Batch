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

namespace Batch.Foreman
{
    public abstract class ForemanBase : IDisposable
    {
        public string PathToSettingsFile;

        private WorkerActivator WorkerActivator;
        private ForemanConfiguration Config;

        private int workerCounter;
        private List<Node> nodes;
        private List<BlockingCollection<object>> queues;
        
        // helper dicts
        private Dictionary<string, int> workerNameToId;
        private Dictionary<string, int> nodeNameToId;
        private Dictionary<string, int> queueNameToId;

        private bool disposed;


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
            if (disposed)
                return;

            if (PathToSettingsFile == null)
                throw new ArgumentNullException("PathToSettingsFile");

            if (!File.Exists(PathToSettingsFile))
                throw new FileNotFoundException(PathToSettingsFile);

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

            workerCounter = 0;
            workerNameToId = new Dictionary<string, int>(Config.workers.Count);
            foreach (var configWorker in Config.workers)
            {
                if (workerNameToId.ContainsKey(configWorker.name))
                    throw new ArgumentException("The worker name '" + configWorker.name + "' is already registered");

                int workerId = workerCounter;
                workerCounter++;

                try
                {
                    WorkerActivator.RegisterWorkerType(workerId, configWorker.className);
                    workerNameToId.Add(configWorker.name, workerId);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Can't create a worker using className (" + ex.Message + ")");
                }
            }

            // Register nodes
            if (Config.nodes == null || Config.nodes.Count == 0)
                throw new ArgumentException("No nodes in config file");

            nodes = new List<Node>(Config.nodes.Count);
            nodeNameToId = new Dictionary<string, int>(Config.nodes.Count);
            foreach (var configNode in Config.nodes)
            {
                string workerName = configNode.worker;
                if (!workerNameToId.ContainsKey(workerName))
                    throw new ArgumentException("The worker name '" + workerName + "' in nodes section is not defined in workers section");
                int workerId = workerNameToId[workerName];
                
                if (nodeNameToId.ContainsKey(configNode.name))
                    throw new ArgumentException("The node name '" + configNode.name + "' is already registered");

                var node = new Node();
                node.id = nodes.Count;
                node.name = configNode.name;
                node.workerId = workerId;
                node.WorkerActivator = WorkerActivator;
                nodes.Add(node);
                nodeNameToId.Add(configNode.name, node.id);
            }

            // Register queues
            if (Config.queues == null || Config.queues.Count == 0)
                throw new ArgumentException("No queues in config file");

            queues = new List<BlockingCollection<object>>(Config.queues.Count);
            queueNameToId = new Dictionary<string, int>(Config.queues.Count);
            foreach (var configQueue in Config.queues)
            {
                if (queueNameToId.ContainsKey(configQueue.name))
                    throw new ArgumentException("The queue name '" + configQueue.name + "' is already registered");

                int queueId = queues.Count;

                if (configQueue.bufferLimit == 0)
                    queues.Add(new BlockingCollection<object>());
                else
                    queues.Add(new BlockingCollection<object>(configQueue.bufferLimit));

                queueNameToId.Add(configQueue.name, queueId);
            }

            // Register connections
            foreach (var configConnection in Config.connections)
            {
                string fromName = configConnection.from;
                string toName = configConnection.to;

                int fromElId, toElId;

                TopologyElementType fromEl = GetNameTopologyType(fromName, out fromElId);
                TopologyElementType toEl = GetNameTopologyType(toName, out toElId);

                // node to node, node to queue and queue to node are supported
                // queue to queue is not supported
                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Queue)
                    throw new Exception("Can't connect a queue to a queue: '" + fromName + "' -> '" + toName + "'");

                if (fromEl == TopologyElementType.Node && toEl == TopologyElementType.Queue)
                {
                    var node = nodes[fromElId];
                    var queue = queues[toElId];
                    node.output = queue;
                }

                if (fromEl == TopologyElementType.Queue && toEl == TopologyElementType.Node)
                {
                    var node = nodes[toElId];
                    var queue = queues[fromElId];
                    node.input = queue;
                }
            }

            DryRun();

            // dispose of helper dicts
            workerNameToId = null;
            nodeNameToId = null;
            queueNameToId = null;
        }

        public void DryRun()
        {
            // iterate over all tree and check if there are any unconnected edges
            // first element must be a node
            // last element must be a node
        }

        public void Run()
        {
            var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            Task[] tasks = new Task[nodes.Count];

            
            foreach (var node in nodes)
            {
                Console.WriteLine("Starting new thread with " + node.name);
                tasks[node.id] = f.StartNew(() => node.Run());
            }

            /*

            for (var i = 0; i < nodes.Count; i++)
            {
                Console.WriteLine("Starting new thread with " + nodes[i].name);

                tasks[i] = f.StartNew(() => nodes[i].Run());
                //nodes[i].Run();

            }

            */
            Task.WaitAll(tasks);    
        }

        public void Dispose()
        {
            disposed = true;
        }

        private TopologyElementType GetNameTopologyType(string Name, out int id)
        {
            int nodeId, queueId, workerId;

            bool isNode = nodeNameToId.TryGetValue(Name, out nodeId);
            bool isQueue = queueNameToId.TryGetValue(Name, out queueId);

            if (isNode && isQueue)
                throw new Exception("The name '" + Name + "' is ambigious - a node or a queue?");

            bool isWorker = workerNameToId.TryGetValue(Name, out workerId);

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
