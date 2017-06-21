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

        IForeman NextForeman { get; set; }

        IForeman TestForeman { get; set; }

        int TestForemanRequestWeight { get; set; }

        string WorkerNodeExceptionString { get; }

        ForemanConfigurationFile Config { get; set; }

        void Load();

        // long running
        void Run();

        // short running
        void Run(ref object Data, bool IsTestForeman = false);

        void Pause();

        void Resume();

        // only for long running foremen
        bool SubmitData(string QueueName, object data);

        // only for long running foremen
        bool CompleteAdding(string QueueName);

        string ExportToConfigString();

        Tuple<long, long, long, TimeSpan> GetAppDomainMonitoringData();
    }
}
