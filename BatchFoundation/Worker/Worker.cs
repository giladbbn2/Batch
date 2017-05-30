using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFoundation.Worker
{
    public class Worker : WorkerBase
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            throw new NotImplementedException("Inherited Worker must override the Run() method");
        }
    }
}
