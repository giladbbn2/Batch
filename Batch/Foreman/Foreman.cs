using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal class Foreman : ForemanBase
    {
        public Foreman(string ConfigString) : base(ConfigString)
        {

        }

        public Foreman(ForemanConfigurationFile Config) : base(Config)
        {

        }
    }
}
