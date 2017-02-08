using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    public class Foreman : ForemanBase
    {
        public Foreman() : base()
        {

        }

        public Foreman(string PathToSettingsFile) : base(PathToSettingsFile)
        {

        }
    }
}
