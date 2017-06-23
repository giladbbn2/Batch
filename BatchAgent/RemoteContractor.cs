using Batch.Contractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Web;

namespace BatchAgent
{
    [ServiceBehavior(
        Name = "BatchRemoteContractor",
        Namespace = "http://schemas.batch.com/agent/remote/contractor",
        IncludeExceptionDetailInFaults = true
    )]
    public class RemoteContractor : IRemoteContractor
    {
        private static Contractor _contractor;
        private static Contractor contractor
        {
            get
            {
                if (_contractor == null)
                    Interlocked.CompareExchange(ref _contractor, new Contractor(), null);

                return _contractor;
            }
        }

        bool IRemoteContractor.GetIsLoaded()
        {
            return contractor.IsLoaded;
        }

        ContractorSettings IRemoteContractor.GetSettings()
        {
            var settings = contractor.Settings;

            var remoteSettings = new ContractorSettings();
            remoteSettings.IsAppDomainMonitoringEnabled = settings.IsAppDomainMonitoringEnabled;
            remoteSettings.ForemanFetchDLLBaseDir = settings.ForemanFetchDLLBaseDir;
            remoteSettings.IsKeepLocalForemanDLL = settings.IsKeepLocalForemanDLL;
            remoteSettings.IsOverwriteLocalForemanDLL = settings.IsOverwriteLocalForemanDLL;
            remoteSettings.ForemanLocalDLLBaseDir = settings.ForemanLocalDLLBaseDir;

            return remoteSettings;
        }

        public void SetSettings(ContractorSettings Settings)
        {
            Batch.Contractor.ContractorSettings local = new Batch.Contractor.ContractorSettings();

            var remoteSettings = new ContractorSettings();
            //local.IsAppDomainMonitoringEnabled = Settings.IsAppDomainMonitoringEnabled;
            local.ForemanFetchDLLBaseDir = Settings.ForemanFetchDLLBaseDir;
            local.IsKeepLocalForemanDLL = Settings.IsKeepLocalForemanDLL;
            local.IsOverwriteLocalForemanDLL = Settings.IsOverwriteLocalForemanDLL;
            local.ForemanLocalDLLBaseDir = Settings.ForemanLocalDLLBaseDir;

            contractor.Settings = local;
        }

        public void AddForeman(string ForemanId, string ConfigString)
        {
            contractor.AddForeman(ForemanId, ConfigString);
        }

        public bool CompleteAdding(string ForemanId, string QueueName)
        {
            return contractor.CompleteAdding(ForemanId, QueueName);
        }

        public void ConnectForeman(string ForemanIdFrom, string ForemanIdTo, bool IsForce = false, bool IsTestForeman = false, int TestForemanRequestWeight = 1000000)
        {
            contractor.ConnectForeman(ForemanIdFrom, ForemanIdTo, IsForce, IsTestForeman, TestForemanRequestWeight);
        }

        public void DisconnectForeman(string ForemanIdFrom, string ForemanIdTo)
        {
            contractor.DisconnectForeman(ForemanIdFrom, ForemanIdTo);
        }

        public string ExportToConfigString()
        {
            return contractor.ExportToConfigString();
        }

        public ForemanStats GetRemoteForemanStats(string ForemanId)
        {
            var stats = contractor.GetForemanStats(ForemanId);

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

        public void ImportFromConfigString(string ConfigString)
        {
            contractor.ImportFromConfigString(ConfigString);
        }

        public void RemoveForeman(string ForemanId)
        {
            contractor.RemoveForeman(ForemanId);
        }

        public object Run(string ForemanId, object Data = null, bool IsFollowConnections = true, bool IsContinueOnError = false)
        {
            contractor.RunObjectByRef(ForemanId, ref Data, IsFollowConnections, IsContinueOnError);
            return Data;
        }

        public bool SubmitData(string ForemanId, string QueueName, object Data)
        {
            return contractor.SubmitData(ForemanId, QueueName, Data);
        }
    }
}