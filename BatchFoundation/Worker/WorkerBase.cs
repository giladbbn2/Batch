using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFoundation.Worker
{
    public abstract class WorkerBase : IDisposable
    {
        public int Id;

        public bool Disposed { get; private set; }



        public WorkerBase()
        {

        }

        #region CreateInstances

        public static WorkerBase CreateInstance(Type type)
        {
            return (WorkerBase)Activator.CreateInstance(type);
        }
             
        public static T CreateInstance<T>() where T : WorkerBase, new()
        {
            T t = new T();
            return t;
        }

        public WorkerBase CreateInstance()
        {
            Type t = this.GetType();
            return (WorkerBase)Activator.CreateInstance(t);
        }

        #endregion

        public virtual void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            throw new NotImplementedException("Inherited Worker must override the Run() method");
        }

        public void Dispose()
        {
            Disposed = true;
        }

    }
}
