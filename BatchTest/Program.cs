﻿using Batch.Foreman;
using Batch.Worker;
using BatchTest;
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
        static void Main(string[] args)
        {
            var t = new Test2();
            t.Run();
        }
    }
}
