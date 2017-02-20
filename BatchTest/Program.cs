using Batch.Foreman;
using Batch.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BatchTest
{
    class Program
    {
        public class MyWorker1 : Worker
        {
            public override object Run(object Item)
            {
                return (object)(Item.ToString() + "|w1|");
            }
        }

        public class MyWorker2 : Worker
        {
            public override object Run(object Item)
            {
                return (object)(Item.ToString() + "|w2|");
            }
        }

        public class MyWorker3 : Worker
        {
            public override object Run(object Item)
            {
                return (object)(Item.ToString() + "|w3|");
            }
        }


        public void Run()
        {
            /*
            Type objType = typeof(MyWorker1);
            //"BatchTest.Program+MyWorker1, BatchTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
            // Print the full assembly name.
            Console.WriteLine(objType.Assembly.FullName.ToString());

            // Print the qualified assembly name.
            Console.WriteLine(objType.AssemblyQualifiedName.ToString());

            Console.ReadLine();
            */

            /*
             
                frmn-test.config:
               

                {
	                "foremanVer": "0.1",
	                "NetMQPort": "",
	                "workers": [{
		                "name": "w1",
		                "className": "BatchTest.Program+MyWorker1, BatchTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
	                }, {
		                "name": "w2",
		                "className": "BatchTest.Program+MyWorker2, BatchTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
	                }, {
		                "name": "w3",
		                "className": "BatchTest.Program+MyWorker3, BatchTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
	                }],
	                "nodes": [{
		                "name": "n1",
		                "worker": "w1",
		                "isFirst": true,
		                "nodeCount": 1
	                }, {
		                "name": "n2",
		                "worker": "w2"
	                }, {
		                "name": "n3",
		                "worker": "w2"
	                }, {
		                "name": "n4",
		                "worker": "w3",
		                "isLast": true
	                }],
	                "queues": [{
		                "name": "q1",
		                "bufferLimit": 5
	                }, {
		                "name": "q2"
	                }],
	                "connections": [{
		                "from": "n1",
		                "to": "q1"
	                }, {
		                "from": "q1",
		                "to": "n2"
	                }, {
		                "from": "q1",
		                "to": "n3"
	                }, {
		                "from": "n2",
		                "to": "q2"
	                }, {
		                "from": "n3",
		                "to": "q2"
	                }, {
		                "from": "q2",
		                "to": "n4"
	                }]
                }
     
            */


            var frmn = new Foreman(@"C:\projects\Batch\Batch\Config\frmn-test.config");
            frmn.LoadSettingsFile();

            var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            f.StartNew(() => {
                try
                {
                    foreach (var item in frmn.lastQueue.GetConsumingEnumerable())
                    {
                        if (item == null)
                            continue;

                        Console.WriteLine(item.ToString());
                    }
                }
                finally
                {
                    frmn.lastQueue.CompleteAdding();
                }
            });

            frmn.firstQueue.Add("1");
            frmn.firstQueue.Add("2");
            frmn.firstQueue.Add("3");
            frmn.firstQueue.CompleteAdding();

            frmn.Run();

            

            

            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }
    }
}
