using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Worker
{
    internal class WorkerNode
    {
        // init
        public int Id;
        public string Guid;
        public string Name;
        public int WorkerId;
        public bool IsFirst;
        public bool IsLast;
        public int NodeCount = 1;
        public WorkerActivator WorkerActivator;

        public BlockingCollection<object> Input;
        public BlockingCollection<object> Output;

        private WorkerBase Worker;

        public bool IsConnected;


        public WorkerNode()
        {

        }

        public void Run()
        {
            try
            {
                foreach (var item in Input.GetConsumingEnumerable())
                {
                    if (item == null)
                        continue;

                    using (Worker = WorkerActivator.CreateWorkerInstance(WorkerId))
                    {
                        object result = Worker.Run(item);
                        if (result != null)
                            Output.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                try
                {
                    Output.CompleteAdding();
                }
                catch (Exception ex)
                {
                    // Output is already complete
                }

            }
        }
    }
}
