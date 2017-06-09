using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    [Serializable]
    public abstract class Data : ICloneable
    {
        public Data()
        {

        }

        public object Clone()
        {
            return new object();
        }
    }
}
