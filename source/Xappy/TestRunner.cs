using System;
using System.Diagnostics;
using System.Linq;

namespace Xappy
{
    public class TestRunner: LogCapableObject
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
            LogInfo(String.Format("Executing vstest commandline:\n{0} {1}", info.FileName, info.Arguments));
            LogDebug(" => start process");
            proc.StartInfo = info;
            proc.Start();
            var result = String.Join("\n", new[] { "Test run commandline: ", info.FileName + " " + info.Arguments, 
                proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd() });
            proc.WaitForExit();
            LogDebug(" => process completed");
            LogDebug(" => process output follows");
            foreach (var line in result.Split('\n').Skip(1))
                LogDebug(String.Format("    {0}", line.Trim()));
            LogDebug(" => end of process output");
            return result;
        }
    }
}
