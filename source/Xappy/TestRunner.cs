using System;
using System.Diagnostics;

namespace Xappy
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
            var result = String.Join("\n", new[] { "Test run commandline: ", info.FileName + " " + info.Arguments, 
                proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd() });
            proc.WaitForExit();
            return result;
        }
    }
}
