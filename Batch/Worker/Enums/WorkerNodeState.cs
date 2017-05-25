using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    internal enum WorkerNodeState : sbyte
    {
        Idle = 0,
        Running = 1,
        Finished = 2,
        Error = 3
    }
}
