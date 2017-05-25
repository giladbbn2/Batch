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

        public void RegisterWorkerType(int WorkerId, string WorkerClassNameWithNamespace)
        {
            if (WorkerClassNameWithNamespace == null)
                throw new ArgumentNullException("WorkerClassNameWithNamespace");

            if (WorkerClassNameWithNamespace.Length == 0)
                throw new ArgumentException("WorkerClassNameWithNamespace must not be zero length");

            if (WorkerTypes == null)
                WorkerTypes = new Dictionary<int, Type>();

            if (!WorkerTypes.ContainsKey(WorkerId))
            {
                Type type = TypeDelegator.GetType(WorkerClassNameWithNamespace);

                if (type == null)
                    throw new Exception(WorkerClassNameWithNamespace + " is supposed to be a class name with namespace");

                WorkerTypes.Add(WorkerId, type);
            }
        }
    }
}
