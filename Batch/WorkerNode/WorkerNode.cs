﻿using Batch.Foreman;
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
        }
    }
}