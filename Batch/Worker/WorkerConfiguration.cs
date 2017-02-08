using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    // Default configuration

    internal class WorkerConfiguration
    {
        // must notify Foreman at least every X seconds
        public int MinNotifySeconds = 5;
    }
}
