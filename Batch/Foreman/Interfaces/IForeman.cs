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

        object Data { get; set; }

        IForeman NextForeman { get; set; }

        IForeman BranchForeman { get; set; }

        int BranchRequestWeight { get; set; }

        void Load();

        void Run();

        void Pause();

        void Resume();

        void StopLongRunningNodeTasks();
    }
}
