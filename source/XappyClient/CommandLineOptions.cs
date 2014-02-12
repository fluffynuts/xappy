using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Parsing;
using CommandLine.Text;

namespace XappyClient
{
    public class CommandLineOptions
    {
        public enum ExitCodes
        {
            Success,
            CommandlineArgumentError,
            Failure,
            ShowedHelp,
            InstallFailed,
            UninstallFailed
        }
        public ExitCodes ExitCode { get; protected set; }
        [Option('h', "help", HelpText = "Show this help")]
        public bool ShowHelp { get; set; }

        [Option('s', "server", HelpText = "Server name")]
        public string Server { get; set; }

        [Option('p', "port", HelpText = "Port")]
        public int Port { get; set; }

        [Option('x', "xap", HelpText = "Your XAP file to test", Required=true)]
        public string XAPFile { get; set; }

        [Option('n', "name", HelpText = "Your build name. When omitted, your XAP file name is used")]
        public string BuildName { get; set; }

        public CommandLineOptions(string[] args, string helpHeading, string copyRightInformation = null)
        {
            this.ExitCode = ExitCodes.CommandlineArgumentError;
            if (Parser.Default.ParseArguments(args, this))
            {
                if (this.ShowHelp)
                {
                    ShowUsage(helpHeading, copyRightInformation);
                    return;
                }
                this.ExitCode = ExitCodes.Success;
            }
            else
            {
                ShowUsage(helpHeading, copyRightInformation);
            }
        }

        private void ShowUsage(string helpHeading, string copyRightInformation)
        {
            var ht = new HelpText(helpHeading);
            ht.AddDashesToOption = true;
            if (!String.IsNullOrWhiteSpace(copyRightInformation))
                ht.Copyright = copyRightInformation;
            ht.AddOptions(this);

            Console.WriteLine(ht.ToString());
            this.ExitCode = ExitCodes.ShowedHelp;
        }
    }
}
