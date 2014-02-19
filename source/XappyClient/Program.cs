using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GIPC;
using TestRunner;
using System.Net;

namespace XappyClient
{
    class Program
    {
        static int Main(string[] args)
        {
            var client = new GIPCClient(GIPCBase.Protocols.NetTcp, "xappy");
            var opts = new CommandLineOptions(args, "Remote Test Runner Help");
            if (opts.ExitCode != CommandLineOptions.ExitCodes.Success)
                return (int)opts.ExitCode;
            client.HostName = opts.Server ?? Dns.GetHostName();
            client.Port = opts.Port < 1 ? 5555 : opts.Port;
            client.CommunicationTimeoutInSeconds = opts.Timeout < 1 ? 600 : opts.Timeout;

            var blob = new Base64Blob(File.ReadAllBytes(opts.XAPFile));
            var name = String.IsNullOrEmpty(opts.BuildName) ? Path.GetFileName(opts.XAPFile) : opts.BuildName;

            var base64String = blob.ToString();
            try
            {
                var result = client.SendMessage(String.Join("\n", new[] { name, base64String }));
                Console.WriteLine(String.Join("\n", result));
                var exitCode = CommandLineOptions.ExitCodes.Success;
                foreach (var line in result)
                {
                    var subLines = line.Split('\n');
                    foreach (var subLine in subLines)
                    {
                        Console.WriteLine(subLine.Trim());
                        if (subLine.ToLower().Trim() == "test run failed.")
                            exitCode = CommandLineOptions.ExitCodes.TestsFailed;
                    }
                }
                return (int)exitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Join("\n", new[] 
                {
                    "Unable to run test at Xappy server:",
                    ex.Message,
                    ex.StackTrace
                }));
                return (int)CommandLineOptions.ExitCodes.Failure;
            }
        }
    }
}
