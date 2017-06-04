using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal interface IForeman
    {
        void Load();

        void Run();

        void Pause();

        void Resume();

        object GetData();

        void SetData(object data);
    }
}
