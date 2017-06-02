using Batch.Foreman;
using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Batch.Worker
{
    internal class WorkerNode
    {
        // init
        public int Id;
        public string Name;
        public int OrderId;
        public object Data;
        public bool IsLongRunning;
        public string WorkerClassName;
        public WorkerBase Worker;
        public WorkerNode NextNode;
        
        public BlockingCollection<object> Input;
        public BlockingCollection<object> Output;

        public bool IsConnected;

        

        public WorkerNode()
        {

        }

        public void Run()
        {
            Worker.Run(Input, Output, ref Data);

            if (NextNode != null)
                NextNode.Data = Data;

            /*
                    
        private volatile bool _IsRunning = false;
        public bool IsRunning
        {
            get;
            private set;
        }
        

            if (IsRunning && IsLongRunning)  // this node is a long running task and is already running so don't run again
                return;

            Thread.MemoryBarrier();

            IsRunning = true;

            Thread.MemoryBarrier();

            WorkerLoader.Run(WorkerClassName, Input, Output, ref Data);

            if (NextNode != null)
                NextNode.Data = Data;

            Thread.MemoryBarrier();

            IsRunning = false;

            Thread.MemoryBarrier();
            */
        }
    }
}
