using Batch.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal class Node
    {
        public int id;
        public string name;
        public int workerId;
        public WorkerActivator WorkerActivator;

        // node to node
        public Node next;
        public object data; // input/output between node to node, should be OUTSIDE of node?

        // node to queue, queue to node
        public BlockingCollection<object> input;
        public BlockingCollection<object> output;

        private WorkerBase worker;

        
        public Node()
        {

        }

        public void Run()
        {
            if (worker == null || worker.disposed)
            {
                worker = WorkerActivator.CreateWorkerInstance(workerId);
                worker.id = workerId;
            }

            worker.Run(input, output, ref data);

            // if node to node then pass data to next node and run it
            if (next != null)
            {
                next.data = data;
                next.Run();
            }
        }
    }
}
