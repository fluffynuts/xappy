using System;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GIPC;
using TestRunner;
using System.Net;
using Xappy.Common;

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

            if (opts.ShowVersionInfo)
            {
                return ShowVersionInfoWith(client);
            }

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

        private static int ShowVersionInfoWith(GIPCClient client)
        {
            var clientVersion = VersionInfo.GetVersion();
            Console.WriteLine("Client version: " + clientVersion);
            try
            {
                var result = client.SendMessage("?version");
                var serverVersion = (result == null || result.Length == 0) ? "unknown" : result.First();
                Console.WriteLine("Server at " + client.HostName + ":" + client.Port + " reports version: " + serverVersion);
            }
            catch
            {
                Console.WriteLine("Unable to determine the version of Xappy server at: " + client.HostName + ":" + client.Port);
            }
            return 0;
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
                        case "error:":
                            StartNewLine();
                            Console.WriteLine(subLine);
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

        private static uint _markCount;

        private static void StartNewLineIfMarksAreTooLong()
        {
            if (_markCount > 80)
            {
                StartNewLine();
            }
        }

        private static void StartNewLine()
        {
            _markCount = 0;
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
            WriteMark("x");
            failed.Add(trimmed);
        }

        private static void WriteMark(string mark)
        {
            StartNewLineIfMarksAreTooLong();
            _markCount++;
            Console.Out.Write(mark);
            Console.Out.Flush();
        }

        private static void WriteSkipped(List<string> skipped, string trimmed)
        {
            WriteMark("-");
            skipped.Add(trimmed);
        }

        private static void WritePassMark()
        {
            WriteMark(".");
        }
    }
}
