using Batch.Foreman;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    public abstract class WorkerBase : IDisposable
    {
        public int id;

        public event EventHandler Started;
        public event EventHandler Alive;
        public event EventHandler Ended;

        private WorkerConfiguration Config;

        public bool disposed { get; private set; }


        public WorkerBase()
        {
            this.Config = new WorkerConfiguration();
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

        protected void OnStarted(EventArgs e)
        {
            if (disposed)
                return;

            Started?.Invoke(this, e);
        }

        protected void OnAlive(EventArgs e)
        {
            if (disposed)
                return;

            Alive?.Invoke(this, e);
        }

        protected void OnEnded(EventArgs e)
        {
            if (disposed)
                return;

            Ended?.Invoke(this, e);
        }

        public void Dispose()
        {
            disposed = true;
            Started = null;
            Alive = null;
            Ended = null;
        }

    }
}
