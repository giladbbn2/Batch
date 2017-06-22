using Batch.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    [Serializable]
    public sealed class ForemanStats
    {
        public string ForemanId
        {
            get;
            private set;
        }
        public long AppDomainMonitoringSurvivedMemorySize
        {
            get;
            private set;
        }
        public long AppDomainMonitoringSurvivedProcessMemorySize
        {
            get;
            private set;
        }
        public long AppDomainMonitoringTotalAllocatedMemorySize
        {
            get;
            private set;
        }
        public TimeSpan AppDomainMonitoringTotalProcessorTime
        {
            get;
            private set;
        }
        public bool IsError
        {
            get;
            private set;
        }
        public string WorkerNodeExceptionString
        {
            get;
            private set;
        }



        public ForemanStats(string ForemanId, long AppDomainMonitoringSurvivedMemorySize, long AppDomainMonitoringSurvivedProcessMemorySize, long AppDomainMonitoringTotalAllocatedMemorySize, TimeSpan AppDomainMonitoringTotalProcessorTime, bool IsError, string WorkerNodeExceptionString)
        {
            this.ForemanId = ForemanId;
            this.AppDomainMonitoringSurvivedMemorySize = AppDomainMonitoringSurvivedMemorySize;
            this.AppDomainMonitoringSurvivedProcessMemorySize = AppDomainMonitoringSurvivedProcessMemorySize;
            this.AppDomainMonitoringTotalAllocatedMemorySize = AppDomainMonitoringTotalAllocatedMemorySize;
            this.AppDomainMonitoringTotalProcessorTime = AppDomainMonitoringTotalProcessorTime;
            this.IsError = IsError;
            this.WorkerNodeExceptionString = WorkerNodeExceptionString;
        }

        public override string ToString()
        {
            return "ForemanId: " + (ForemanId != null ? ForemanId : "") + "\nAppDomainMonitoringSurvivedMemorySize: " + AppDomainMonitoringSurvivedMemorySize.ToString() + "\nAppDomainMonitoringSurvivedProcessMemorySize: " + AppDomainMonitoringSurvivedProcessMemorySize.ToString() + "\nAppDomainMonitoringTotalAllocatedMemorySize: " + AppDomainMonitoringTotalAllocatedMemorySize.ToString() + "\nAppDomainMonitoringTotalProcessorTime: " + AppDomainMonitoringTotalProcessorTime.ToString() + "\nIsError: " + (IsError ? "True" : "False") + "\nWorkerNodeExceptionString: " + (WorkerNodeExceptionString != null ? WorkerNodeExceptionString : "");
        }
    }
}
