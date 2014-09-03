using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xappy.Common
{
    public class VersionInfo
    {
        public static string GetVersion()
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName();
            var ver = assemblyName.Version;
            return string.Join(".", new[] { ver.Major, ver.Minor });
        }
    }
}
