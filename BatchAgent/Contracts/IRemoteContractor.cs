using Batch.Contractor;
using Batch.Foreman;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace BatchAgent
{
    [ServiceContract(
        Name = "BatchRemoteContractor",
        Namespace = "http://schemas.batch.com/agent/remote/contractor"
    )]
    public interface IRemoteContractor
    {
        // added
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        ContractorSettings GetSettings();

        // added
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        void SetSettings(ContractorSettings Settings);

        // added
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        bool GetIsLoaded();

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        void ImportFromConfigString(string ConfigString);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        string ExportToConfigString();

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        void AddForeman(string ForemanId, string ConfigString);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        void RemoveForeman(string ForemanId);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        void ConnectForeman(string ForemanIdFrom, string ForemanIdTo, bool IsForce = false, bool IsTestForeman = false, int TestForemanRequestWeight = 1000000);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        void DisconnectForeman(string ForemanIdFrom, string ForemanIdTo);

        // changed
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        object Run(string ForemanId, object Data = null, bool IsFollowConnections = true, bool IsContinueOnError = false);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        bool SubmitData(string ForemanId, string QueueName, object Data);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        bool CompleteAdding(string ForemanId, string QueueName);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        [WebInvoke(Method = "*")]
        ForemanStats GetRemoteForemanStats(string ForemanId);
    }
}
