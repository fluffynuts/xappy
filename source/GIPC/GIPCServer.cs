using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;

namespace GIPC
{
    public delegate string MessageCallback(string message);
    public class ProtocolNotImplementedException : Exception
    {
        public ProtocolNotImplementedException(string message)
            : base(message)
        { }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class GIPCServer : GIPCBase, IGIPCServer, IDisposable
    {
        public event MessageCallback OnMessageReceived = null;

        private void RestartIfRequired()
        {
            bool restart = this.Running;
            if (restart)
                this.Stop();
            if (restart)
                this.Start();
        }

        public bool Running
        {
            get
            {
                lock (this)
                {
                    return this._host != null;
                }
            }
        }
        private ServiceHost _host = null;

        public GIPCServer()
        {
            this.init(_defaultProtocol, _defaultIdentifier);
        }

        public GIPCServer(Protocols protocol, string identifier)
        {
            this.init(protocol, identifier);
        }

        private void init(Protocols protocol, string identifier)
        {
            this._protocol = protocol;
            this._identifier = identifier;
            this.ParameterChanged += (s, e) => RestartIfRequired();
        }

        public string[] SendMessage(string message)
        {
            var ret = new List<string>();
            if (this.OnMessageReceived == null) return null;
            foreach (var d in this.OnMessageReceived.GetInvocationList())
            {
                var dret = d.DynamicInvoke(message) as String;
                ret.Add(dret);
            }
            return ret.ToArray();
        }

        public void Start()
        {
            lock (this)
            {
                this.Stop();
                Action<Action> tryOrThrow = (act) =>
                    {
                        try { act(); }
                        catch { this.Stop(); throw; }
                    };
                switch (this._protocol)
                {
                    case Protocols.NamedPipe:
                        tryOrThrow(StartNamedPipeServer);
                        break;
                    case Protocols.NetTcp:
                        tryOrThrow(StartNetTcpServer);
                        break;
                    default:
                        throw new ProtocolNotImplementedException("GIPC service protocol: " + this._protocol + " not implemented (yet)");
                }
            }
        }

        private void StartNetTcpServer()
        {
            var uri = new Uri(this.GetNetTcpProtocolAndHost());
            this._host = new ServiceHost(this, new Uri[] { uri });
            var endPointName = GenerateEndPointName();
            this._host.AddServiceEndpoint(typeof(IGIPCServer), CreateNetTcpBinding(), endPointName);
            this._host.Open();
        }

        private void StartNamedPipeServer()
        {
            var uri = new Uri(GetNamedPipeProtocolAndHost());
            this._host = new ServiceHost(this, new Uri[] {uri});
            var endpointName = this.GenerateEndPointName();
            this._host.AddServiceEndpoint(typeof (IGIPCServer), new NetNamedPipeBinding(), endpointName);
            this._host.Open();
        }

        public void Stop()
        {
            lock (this)
            {
                if (this._host != null)
                {
                    try
                    {
                        this._host.Close();
                    }
                    finally
                    {
                        this._host = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}
