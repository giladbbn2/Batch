using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchTestBL.Test6
{
    public class NumberHolder : MarshalByRefObject
    {
        public int Number;
    }

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            dynamic d = Data;
            d.Number++;
            Data = (object)d;

            //Data = (object)((int)Data + 1);

            Console.WriteLine("Inside Worker1");

            //Console.WriteLine(d.Number);
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            dynamic d = Data;
            d.Number += 100;
            Data = (object)d;

            //Data = (object)((int)Data + 100);

            Console.WriteLine("Inside Worker2");

            //Console.WriteLine(d.Number);
        }
    }
}
