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
using TestRunner;

namespace Xappy
{
    public class TestRunnerMarshal: IDisposable
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
        }

        public void Stop()
        {
            lock (this)
            {
                if (_server != null)
                    _server.Dispose();
                _server = null;
            }
        }

        private string OnServerMessageReceived(string message)
        {
            var testRunner = new TestRunner(ConfigurationManager.AppSettings["vstest"]);
            var parts = message.Split('\n');
            var buildName = parts[0];
            _onBusy("Starting run for: " + buildName);
            var tempFile = Path.GetTempFileName() + ".xap";
            var base64Data = String.Join("\n", parts.Skip(1));
            var blob = new Base64Blob(base64Data);
            File.WriteAllBytes(tempFile, blob.ToBinary());

            try
            {
                var result = testRunner.ProcessBuild(tempFile);
                _onCompleted(null);
                return result;
            }
            finally
            {
                try { 
                //    File.Delete(tempFile); 
                }
                catch { }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
