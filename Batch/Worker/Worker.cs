using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    public class Worker : WorkerBase
    {
        public override object Run(object Item)
        {
            throw new NotImplementedException("Inherited Worker must override the Run() method");
        }
    }
}
