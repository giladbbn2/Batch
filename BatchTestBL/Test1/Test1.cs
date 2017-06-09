using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BatchTestBL.Test1
{
    public static class Settings
    {
        public static bool IsTestWithSleep = true;
    }

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            /*
            Console.WriteLine(DateTime.UtcNow + " - W1: " + data);

            string[] strings = new string[] { "a", "b", "c", "d", "e", "f", "g" };

            foreach (var str in strings)
            {
                Output.Add((object)str);

                if (Settings.IsTestWithSleep)
                    Thread.Sleep(1000);
            }

            Output.CompleteAdding();

            Console.WriteLine(DateTime.UtcNow + " - W1 ended");
            */

            // do nothing

            Console.WriteLine("W1 was here");
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine(DateTime.UtcNow + " - W2: " + data);

            try
            {
                foreach (var item in Input.GetConsumingEnumerable())
                {
                    string str = item.ToString() + "1";
                    Output.Add((object)str);

                    if (Settings.IsTestWithSleep)
                        Thread.Sleep(1000);
                }
            }
            finally
            {
                Output.CompleteAdding();
            }

            Console.WriteLine(DateTime.UtcNow + " - W2 ended");
        }
    }

    public class MyWorker3 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine(DateTime.UtcNow + " - W3: " + data);

            foreach (var item in Input.GetConsumingEnumerable())
            {
                string str = item.ToString() + "g";
                Console.WriteLine(str);

                if (Settings.IsTestWithSleep)
                    Thread.Sleep(1000);
            }

            Console.WriteLine(DateTime.UtcNow + " - W3 ended");
        }
    }

    public class MyWorker4 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w5 started");

            string val = (new Random()).Next(1000).ToString();

            data = (object)val;

            Console.WriteLine("sending in data: " + val);

            Console.WriteLine("w5 ended");
        }
    }

    public class MyWorker5 : Worker
    {
        public static int counter;

        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w6/7 started");

            Interlocked.Increment(ref counter);

            string val = data.ToString();

            Console.WriteLine("rcvd in data: " + val);
            Console.WriteLine("counter: " + counter.ToString());
            Console.WriteLine("w6/7 ended");
        }
    }
}
