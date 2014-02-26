using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GIPC;
using log4net.Config;
using TestRunner;

namespace Xappy
{
    public class TestRunnerMarshal: LogCapableObject, IDisposable
    {
        private static GIPCServer _server;
        private Action<string> _onBusy;
        private Action<string> _onCompleted;
        public string Host { get; protected set; }
        public int Port { get; protected set; }
        public int TimeoutInSeconds { get; protected set; }

        public TestRunnerMarshal(Action<string> onBusy, Action<string> onCompleted)
        {
            _onBusy = onBusy;
            _onCompleted = onCompleted;
        }

        public bool Start()
        {
            GetGIPCServerConfigValues();
            SetupGIPCServerWith(Host, Port, TimeoutInSeconds);
            try
            {
                _server.Start();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Join("\n", new[]
                                                  {
                                                      "Unable to start GIPC server to listen for incoming requests:", ex.Message, ex.StackTrace
                                                  }));
                return false;
            }
        }

        private void SetupGIPCServerWith(string host, int port, int timeout)
        {
            _server = new GIPCServer(GIPCBase.Protocols.NetTcp, "xappy")
                      {
                          HostName = host,
                          Port = port,
                          MaxMessageSizeInMB = 25,
                          CommunicationTimeoutInSeconds = timeout
                      };
            _server.OnMessageReceived += OnServerMessageReceived;
        }

        private void GetGIPCServerConfigValues()
        {
            var appSettings = ConfigurationManager.AppSettings;
            GetServerHostAndPort(appSettings);
            GetTimeoutValue(appSettings);
        }

        private void GetTimeoutValue(NameValueCollection appSettings)
        {
            TimeoutInSeconds = 600;
            var timeout = appSettings["timeout"];
            if (!String.IsNullOrEmpty(timeout))
            {
                int timeoutValue;
                if (int.TryParse(timeout, out timeoutValue))
                {
                    if (timeoutValue > 1)
                        TimeoutInSeconds = timeoutValue;
                }
            }
        }

        private void GetServerHostAndPort(NameValueCollection appSettings)
        {
            var listen = appSettings["listen"] ?? String.Join(":", new[] {Dns.GetHostName(), "5555"});
            var parts = listen.Split(':');
            var host = Dns.GetHostName();
            var port = 5555;
            var portFound = false;
            var hostnameFound = false;
            foreach (var item in parts)
            {
                if (!portFound && int.TryParse(item, out port))
                {
                    portFound = true;
                    continue;
                }
                if (!hostnameFound)
                {
                    host = item;
                    hostnameFound = true;
                }
            }
            this.Host = host;
            this.Port = port;
            LogDebug(String.Format("Listening on {0}:{1}", Host, Port));
        }

        public void Stop()
        {
            lock (this)
            {
                LogDebug("Stopping GIPC server");
                if (_server != null)
                    _server.Dispose();
                _server = null;
                LogDebug("GIPC server stopped");
            }
        }

        private string OnServerMessageReceived(string message)
        {
            var testRunner = new TestRunner(ConfigurationManager.AppSettings["vstest"]);
            var parts = message.Split('\n');
            var buildName = parts[0];
            _onBusy("Starting run for: " + buildName);
            LogInfo(String.Format("Test run request received for: {0}", buildName));
            var tempFile = Path.GetTempFileName() + ".xap";
            var base64Data = String.Join("\n", parts.Skip(1));
            var blob = new Base64Blob(base64Data);
            var xapBytes = blob.ToBinary();
            LogDebug(String.Format("Creating temporary XAP at: {0} ({1}k)", tempFile, (xapBytes.Length / 1024)));
            File.WriteAllBytes(tempFile, xapBytes);
            LogDebug(" => temporary file created");

            try
            {
                var result = testRunner.ProcessBuild(tempFile);
                _onCompleted(GetLastRunMessage());
                return result;
            }
            catch (Exception ex)
            {
                LogError("Error running test:");
                LogError(ex.Message + "\n\n" + ex.StackTrace);
                _onCompleted(GetLastFailedMessage());
                return "Unable to run tests: " + ex.Message;
            }
            finally
            {
                try {
                    LogDebug("Removing temporary XAP file");
                    File.Delete(tempFile);
                    LogDebug(" => done");
                }
                catch { }
            }
        }

        private string GetLastFailedMessage()
        {
            return GetLastMessageFor("failure");
        }

        private string GetLastMessageFor(string operation)
        {
            return String.Format("Listening on {0}:{1}  Last {3} was at: {2}", Host, Port, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), operation);
        }
        private string GetLastRunMessage()
        {
            return GetLastMessageFor("successful run");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
