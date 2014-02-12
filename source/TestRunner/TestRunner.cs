using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunner
{
    public class TestRunner
    {
        private readonly string _vsTestPath;

        public TestRunner(string vsTestPath)
        {
            _vsTestPath = vsTestPath;
        }

        public string ProcessBuild(string parameters)
        {
            var proc = new Process();
            var info = new ProcessStartInfo();
            info.FileName = _vsTestPath;
            info.Arguments = parameters + " /InIsolation";
            info.RedirectStandardError = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            proc.StartInfo = info;
            proc.Start();
            proc.WaitForExit();
            return String.Join("\n", new[] { proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd() });
        }
    }
}
