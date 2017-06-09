using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchTestBL.Test3
{
    // will work either with SerializableAttribute or with by extending MarshalByRefObject
    // class Person is init'd from the outside, and passed to the workers

    public class Person : MarshalByRefObject
    {
        public int x
        {
            get;
            set;
        }



        public Person()
        {
            x = 0;
        }

        public void addOne()
        {
            x++;
        }

    }

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            //Person p = (Person)data;
            //p.x++;
            //Console.WriteLine(DateTime.UtcNow + " - W1: " + p.x);

            dynamic d = data;
            d.x++;

            Console.WriteLine(DateTime.UtcNow + " - W1");
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            //Person p = (Person)data;
            //p.x++;
            //Console.WriteLine(DateTime.UtcNow + " - W2: " + p.x);

            dynamic d = data;
            //d.x++;
            d.addOne();
            //d.addOne();

            // this will trigger an error
            //try
            //{
                d.addOne2();
            //}
            //catch (Exception ex)
            //{
                //Console.WriteLine(ex.Message);
            //}

            Console.WriteLine(DateTime.UtcNow + " - W2");
        }
    }

    public class MyWorker3 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {

            dynamic d = data;
            d.x++;
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();


            Console.WriteLine(DateTime.UtcNow + " - W3");
        }
    }
}
