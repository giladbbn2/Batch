using Batch.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal interface IForeman
    {
        string Id { get; set; }

        bool IsNodesLongRunning { get; }

        bool IsError { get; }

        object Data { get; set; }

        IForeman NextForeman { get; set; }

        IForeman TestForeman { get; set; }

        int TestForemanRequestWeight { get; set; }

        IEnumerable<WorkerNodeState> WorkerNodeStates { get; }

        void Load();

        void Run(bool IsTestForeman);

        void Pause();

        void Resume();

        // only for long running foremen
        bool SubmitData(string QueueName, object data);

        // only for long running foremen
        bool CompleteAdding(string QueueName);
    }
}
