using Batch.Contractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;

namespace BatchAgent
{
    [ServiceBehavior(
        Name = "BatchRemoteContractor",
        Namespace = "http://schemas.batch.com/agent/remote/contractor"
    )]
    public class RemoteContractor : ContractorBase, IRemoteContractor
    {
        public ForemanStats GetRemoteForemanStats(string ForemanId)
        {
            var stats = GetForemanStats(ForemanId);

            var remoteStats = new ForemanStats();
            remoteStats.ForemanId = stats.ForemanId;
            remoteStats.AppDomainMonitoringSurvivedMemorySize = stats.AppDomainMonitoringSurvivedMemorySize;
            remoteStats.AppDomainMonitoringSurvivedProcessMemorySize = stats.AppDomainMonitoringSurvivedProcessMemorySize;
            remoteStats.AppDomainMonitoringTotalAllocatedMemorySize = stats.AppDomainMonitoringTotalAllocatedMemorySize;
            remoteStats.AppDomainMonitoringTotalProcessorTime = stats.AppDomainMonitoringTotalProcessorTime;
            remoteStats.IsError = stats.IsError;
            remoteStats.WorkerNodeExceptionString = stats.WorkerNodeExceptionString;

            return remoteStats;
        }
    }
}