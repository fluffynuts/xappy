using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Xappy
{
    public class TestRunner: LogCapableObject
    {
        private readonly string _vsTestPath;

        public TestRunner(string vsTestPath)
        {
            _vsTestPath = vsTestPath;
        }

        public string ProcessBuild_StreamsConnected(string parameters)
        {
            var proc = new Process();
            var info = new ProcessStartInfo();
            if (!File.Exists(_vsTestPath))
            {
                throw new Exception("Unable to find the vstest binary at the configuration location: '" + _vsTestPath + "'");
            }
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

        public string ProcessBuild(string parameters)
        {
            return ProcessBuild_ViaIntermediateFile(parameters);
        }

        public string ProcessBuild_ViaIntermediateFile(string parameters)
        {
            if (!File.Exists(_vsTestPath))
            {
                throw new Exception("Unable to find the vstest binary at the configuration location: '" + _vsTestPath + "'");
            }
            try
            {
                using (var executor = new ExecutorBatchFileWrapper(_vsTestPath, parameters))
                {
                    LogDebug("=> starting test process");
                    var result = executor.Execute();
                    LogDebug("=> test process completed");
                    return result;
                }
            }
            catch (Exception ex)
            {
                return String.Join("\n", new[] { "Unable to run tests:", ex.Message, ex.StackTrace });
            }
        }

    }

    public class ExecutorBatchFileWrapper : IDisposable
    {
        private readonly string _tempOutputFile;
        private readonly string _tempBatchFile;

        public ExecutorBatchFileWrapper(string vsTestPath, string parameters)
        {
            _tempOutputFile = Path.GetTempFileName();
            _tempBatchFile = Path.GetTempFileName() + ".bat";
            var cmd = String.Format("\"{0}\" /InIsolation {1} > \"{2}\" 2>&1", vsTestPath, parameters, _tempOutputFile);
            File.WriteAllBytes(_tempBatchFile, Encoding.UTF8.GetBytes(cmd));
        }

        public string Execute()
        {
            using (var proc = CreateProcessToRunTemporaryBatchFile())
            {
                proc.Start();
                proc.WaitForExit();
                var result = String.Join("\n", new[] { "Test run commandline: ", proc.StartInfo.FileName + " " + proc.StartInfo.Arguments, 
                ReadContentsFromTempOutputFile() });
                return result;
            }
        }

        private Process CreateProcessToRunTemporaryBatchFile()
        {
            return new Process()
                   {
                       StartInfo = new ProcessStartInfo 
                                   {
                                       FileName = _tempBatchFile, 
                                       RedirectStandardError = false, 
                                       RedirectStandardInput = false, 
                                       RedirectStandardOutput = false, 
                                       UseShellExecute = true, 
                                       WindowStyle = ProcessWindowStyle.Hidden
                                   }
                   };
        }

        private string ReadContentsFromTempOutputFile()
        {
            return Encoding.UTF8.GetString(File.ReadAllBytes(_tempOutputFile));
        }

        public void Dispose()
        {
            lock (this)
            {
                foreach (var f in new[] { _tempOutputFile, _tempBatchFile })
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch { }
                }
            }
        }
    }
}
