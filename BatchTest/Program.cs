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
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w1 Started");

                string[] strings = new string[] { "a", "b", "c", "d", "e", "f", "g" };

                foreach (var str in strings)
                {
                    Output.Add((object)str);
                    Thread.Sleep(1000);
                }

                Output.CompleteAdding();

                Console.WriteLine("w1 Ended");
            }
        }

        public class MyWorker2 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w2/3 Started");

                try
                {
                    foreach (var item in Input.GetConsumingEnumerable())
                    {
                        string str = item.ToString() + "1";
                        Output.Add((object)str);
                        Thread.Sleep(1000);
                    }
                }
                finally
                {
                    Output.CompleteAdding();
                }

                Console.WriteLine("w2/3 Ended");
            }
        }

        public class MyWorker3 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w4 Started");

                foreach (var item in Input.GetConsumingEnumerable())
                {
                    string str = item.ToString() + "g";
                    Console.WriteLine(str);
                    Thread.Sleep(1000);
                }

                Console.WriteLine("w4 Ended");
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
            var frmn = new Foreman(@"C:\projects\Batch\Batch\Config\frmn-test.config");
            frmn.LoadSettingsFile();
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
