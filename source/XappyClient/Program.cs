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
                var exitCode = CommandLineOptions.ExitCodes.Success;
                var failed = new List<string>();
                var skipped = new List<string>();
                exitCode = GetValue(result, skipped, failed);
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

        private static CommandLineOptions.ExitCodes GetValue(string[] result, List<string> skipped, List<string> failed)
        {
            var exitCode = CommandLineOptions.ExitCodes.Success;
            foreach (var line in result)
            {
                var subLines = line.Split('\n');
                string lastPrefix = null;
                foreach (var subLine in subLines)
                {
                    var trimmed = subLine.Trim();
                    var firstWord = trimmed.Split(' ').First().ToLower();
                    switch (firstWord)
                    {
                        case "passed":
                            WritePassMark();
                            lastPrefix = firstWord;
                            break;
                        case "skipped":
                            WriteSkipped(skipped, trimmed);
                            lastPrefix = firstWord;
                            break;
                        case "failed":
                            WriteFailed(failed, trimmed);
                            lastPrefix = firstWord;
                            break;
                        case "total":
                            StartNewLine();
                            ReportSkippedAndFailed(skipped, failed);
                            Console.WriteLine(trimmed);
                            lastPrefix = firstWord;
                            break;
                        default:
                            switch (lastPrefix)
                            {
                                case "failed":
                                    failed.Add("\t" + subLine);
                                    break;
                                default:
                                    Console.WriteLine(subLine);
                                    break;
                            }
                            break;
                    }
                    if (trimmed.ToLower() == "test run failed.")
                        exitCode = CommandLineOptions.ExitCodes.TestsFailed;
                }
            }
            return exitCode;
        }

        private static void StartNewLine()
        {
            Console.Write("\n");
            Console.Out.Flush();
        }

        private static void ReportSkippedAndFailed(List<string> skipped, List<string> failed)
        {
            var hasFailed = failed.Any();
            var hasSkipped = skipped.Any();
            if (hasFailed || hasSkipped)
            {
                Console.WriteLine("Interesting results:");
                if (hasSkipped)
                {
                    Console.WriteLine(String.Join("\n", skipped));
                }
                if (hasFailed)
                {
                    Console.WriteLine(String.Join("\n", failed));
                }
            }
        }

        private static void WriteFailed(List<string> failed, string trimmed)
        {
            Console.Out.Write("x");
            Console.Out.Flush();
            failed.Add(trimmed);
        }

        private static void WriteSkipped(List<string> skipped, string trimmed)
        {
            Console.Out.Write("-");
            Console.Out.Flush();
            skipped.Add(trimmed);
        }

        private static void WritePassMark()
        {
            Console.Out.Write(".");
            Console.Out.Flush();
        }
    }
}
