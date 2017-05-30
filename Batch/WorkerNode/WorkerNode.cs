using Batch.Foreman;
using BatchFoundation.Worker;
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
        public int OrderId;
        public object Data;
        public bool IsWaitToFinish;

        public WorkerNode NextNode;
        public WorkerActivator WorkerActivator;

        public BlockingCollection<object> Input;
        public BlockingCollection<object> Output;

        public bool IsConnected;

        private WorkerBase Worker;



        public WorkerNode()
        {

        }

        public void Run()
        {
            using (Worker = WorkerActivator.CreateWorkerInstance(WorkerId))
                Worker.Run(Input, Output, ref Data);

            if (NextNode != null)
                NextNode.Data = Data;
        }
    }
}
