using BatchFoundation.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    internal class WorkerActivator
    {
        // worker id to a function that creates an instance of it
        private Dictionary<int, Type> WorkerTypes;



        public WorkerActivator()
        {
            
        }

        /// <summary>
        /// Create a new worker instance, use only when worker is already registered
        /// </summary>
        /// <param name="WorkerId"></param>
        /// <returns></returns>
        public WorkerBase CreateWorkerInstance(int WorkerId)
        {
            if (WorkerTypes == null)
                throw new ArgumentNullException("WorkerActivators");

            Type type;
            if (!WorkerTypes.TryGetValue(WorkerId, out type))
                throw new Exception("WorkerId: " + WorkerId.ToString() + " is not registered");

            return (WorkerBase)Activator.CreateInstance(type);
        }

        public void RegisterWorkerType(int WorkerId, Type WorkerType)
        {
            if (WorkerType == null)
                throw new ArgumentNullException("WorkerType");

            if (WorkerTypes == null)
                WorkerTypes = new Dictionary<int, Type>();

            //Type type = TypeDelegator.GetType(WorkerClassNameWithNamespace);

            if (!WorkerTypes.ContainsKey(WorkerId))
                WorkerTypes.Add(WorkerId, WorkerType);
        }
    }
}
