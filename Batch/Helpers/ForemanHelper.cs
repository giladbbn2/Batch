using Batch.Foreman;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Batch
{
    internal static class ForemanHelper
    {
        public static ForemanConfigurationFile LoadConfigFile(string PathToConfigFile)
        {
            ForemanConfigurationFile Config;

            try
            {
                string settings = File.ReadAllText(PathToConfigFile);
                Config = JsonConvert.DeserializeObject<ForemanConfigurationFile>(settings);
            }
            catch (Exception ex)
            {
                string err = "Can't parse config file: " + PathToConfigFile + "(" + ex.Message + ")";
                throw new Exception(err, ex);
            }

            if (Config.foremanId.Length == 0)
                throw new ArgumentException("foremanId must not be empty string");

            return Config;
        }

        public static Dictionary<string, Assembly> LoadAssemblies(IEnumerable<ForemanConfigurationFile> Configs)
        {
            if (Configs == null)
                throw new ArgumentNullException("Configs");

            var assemblies = new Dictionary<string, Assembly>();      // key is assembly name
            var paths = new Dictionary<string, string>();             // key is path, val is assembly name

            foreach (var config in Configs)
            {
                if (config.assemblies == null || config.assemblies.Count == 0)
                    continue;

                foreach (var configAssembly in config.assemblies)
                {
                    if (assemblies.ContainsKey(configAssembly.name))
                        throw new ArgumentException("Assembly names across all foremen must be unique");

                    string assemblyName;
                    if (paths.TryGetValue(configAssembly.path, out assemblyName))
                    {
                        assemblies.Add(configAssembly.name, assemblies[assemblyName]);
                    }
                    else
                    {
                        assemblies.Add(configAssembly.name, Assembly.LoadFile(configAssembly.path));
                        paths.Add(configAssembly.path, configAssembly.name);
                    }
                }
            }

            return assemblies;
        }
    }
}
