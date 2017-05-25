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
    class Test2
    {
        private string ConfigFile = @"C:\projects\Batch\Config\frmn-test2.config";
        private static int num;

        public class MyWorker1 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w1+");
                num = (new Random()).Next(1000);
                Console.WriteLine(num);
                Console.WriteLine("w1-");
            }
        }

        public class MyWorker2 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w2+");
                num++;
                Console.WriteLine(num);
                Console.WriteLine("w2-");
            }
        }

        public class MyWorker3 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w3+");
                num++;
                Console.WriteLine(num);
                Console.WriteLine("w3-");
            }
        }

        public class MyWorker4 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w4+");
                num++;
                Console.WriteLine(num);
                Console.WriteLine("w4-");
            }
        }

        public class MyWorker5 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w5+");
                num++;
                Console.WriteLine(num);
                Console.WriteLine("w5-");
            }
        }

        public class MyWorker6 : Worker
        {
            public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
            {
                Console.WriteLine("w6+");
                Console.WriteLine(data.ToString() + "123");
                Thread.Sleep(3000);
                Console.WriteLine("w6-");
            }
        }

        public void Run()
        {
            var frmn = new Foreman(ConfigFile);
            frmn.Load();
            frmn.Run();

            Console.ReadLine();
        }
    }
}
