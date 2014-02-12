using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace GIPC
{
    public class GIPCClient : GIPCBase, IDisposable
    {
        private void ReconnectIfRequired()
        {
            if (!this.Connected)
                return;
            this.Disconnect();
            this.Connect();
        }

        public virtual bool Connected {
            get { return (this._channelFactory != null); }
        }

        private ChannelFactory<IGIPCServer> _channelFactory = null;

        public GIPCClient()
        {
            Init(_defaultProtocol, _defaultIdentifier);
        }

        public GIPCClient(Protocols protocol, string identifier)
        {
            Init(protocol, identifier);
        }

        private void Init(Protocols withProtocol, string withIdentifier)
        {
            this._protocol = withProtocol;
            this._identifier = withIdentifier;
            this.ParameterChanged += (s, e) => ReconnectIfRequired();
        }

        public virtual void Connect(Protocols? protocol = null, string identifier = null)
        {
            var reconfigure = false;
            if (protocol.HasValue && protocol.Value != this._protocol)
            {
                this._protocol = protocol.Value;
                reconfigure = true;
            }
            if ((identifier != null) && (identifier != this._identifier))
            {
                this._identifier = identifier;
                reconfigure = true;
            }
            if (this.Connected && reconfigure)
                this.Disconnect();
            if (this.Connected)
                return;
            switch (this.Protocol)
            {
                case Protocols.NamedPipe:
                    this._channelFactory = new ChannelFactory<IGIPCServer>(new NetNamedPipeBinding(), 
                        new EndpointAddress(this.GenerateEndPointAddress()));
                    break;
                case Protocols.NetTcp:
                    this._channelFactory = new ChannelFactory<IGIPCServer>(CreateNetTcpBinding(),
                        new EndpointAddress(this.GenerateEndPointAddress()));
                    break;
                default:
                    throw new ProtocolNotImplementedException("GIPC client protocol: " + this._protocol + " not implemented (yet)");
            }
        }

        public virtual void Disconnect()
        {
            lock (this)
            {
                if (this._channelFactory != null)
                {
                    try
                    {
                        this._channelFactory.Close();
                    }
                    catch   // server could have closed down beforehand: suppress errors on channel factory close
                    {
                    }
                    finally
                    {
                        this._channelFactory = null;
                    }
                }
            }
        }

        public virtual string[] SendMessage(string message, Protocols? protocol = null, string identifier = null)
        {
            string[] ret = null;
            this.Connect(protocol, identifier);
            var pipe = this._channelFactory.CreateChannel();
            if (pipe != null)
                ret = pipe.SendMessage(message);
            return ret;
        }

        public virtual void Dispose()
        {
         	this.Disconnect();
        }
    }
}
