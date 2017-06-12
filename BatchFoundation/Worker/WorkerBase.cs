using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFoundation.Worker
{
    public abstract class WorkerBase : IDisposable
    {
        public int Id;
        public string ClassName;

        public bool IsTest;
        public bool IsDisposed { get; private set; }



        public WorkerBase()
        {

        }

        public virtual void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            throw new NotImplementedException("Inherited Worker must override the Run() method");
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
