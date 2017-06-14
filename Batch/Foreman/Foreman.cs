using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal class Foreman : ForemanBase
    {
        public Foreman(string PathToConfigFile) : base(PathToConfigFile)
        {

        }

        public Foreman(ForemanConfigurationFile Config) : base(Config)
        {

        }
    }
}
