using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal class Foreman : ForemanBase
    {
        public Foreman(string Id, string ConfigString) : base(Id, ConfigString)
        {

        }

        public Foreman(string Id, ForemanConfigurationFile Config) : base(Id, Config)
        {

        }
    }
}
