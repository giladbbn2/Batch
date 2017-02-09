using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    internal class WorkerNode
    {
        // init
        public int Id;
        public string Name;
        public int WorkerId;
        public WorkerActivator WorkerActivator;

        // node to node
        public WorkerNode NextNode;
        public object Data; // input/output between node to node, should be OUTSIDE of node?

        // node to queue, queue to node
        public BlockingCollection<object> Input;
        public BlockingCollection<object> Output;

        private WorkerBase Worker;

        public bool IsConnected;


        public WorkerNode()
        {

        }

        public void Run()
        {
            using (Worker = WorkerActivator.CreateWorkerInstance(WorkerId))
            {
                Worker.Run(Input, Output, ref Data);
            }

            // if node to node then pass data to next node and run it
            if (NextNode != null)
            {
                NextNode.Data = Data;
                NextNode.Run();
            }
        }
    }
}
