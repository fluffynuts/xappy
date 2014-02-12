using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace GIPC
{
    public class GIPCBase
    {
        public const string NamedPipeProtocol = "net.pipe://";
        public const string NetTcpProtocol = "net.tcp://";
        public const string DefaultHostName = "localhost";
        public const string AddressBaseName = "Chillisoft";

        public decimal MaxMessageSizeInMB { get; set; }

        public enum Protocols
        {
            None,
            NamedPipe,
            NetTcp
        }

        public Protocols Protocol 
        { 
            get
            {
                return _protocol;
            }
            set
            {
                var changed = value != _protocol;
                _protocol = value;
                if (changed)
                    RaiseParameterChangedEvent();
            }
        }
        protected Protocols _protocol;
        public bool IdentifierSanitised { get; protected set; }

        public string Identifier 
        { 
            get
            {
                return this._identifier;
            }
            set
            {
                var newValue = this.SanitiseIdentifier(value);
                this.IdentifierSanitised = (newValue != value);
                var changed = newValue != this._identifier;
                this._identifier = newValue;
                if (changed)
                    RaiseParameterChangedEvent();
            } 
        }

        protected string _identifier;

        private void RaiseParameterChangedEvent([CallerMemberName] string changedParameter = "")
        {
            if (ParameterChanged != null)
                ParameterChanged(this, new ParameterChangedEventArgs(changedParameter));
        }

        public delegate void ParameterChangedEventHandler(object sender, ParameterChangedEventArgs args);

        public ParameterChangedEventHandler ParameterChanged { get; set; }

        protected string _hostName;
        public string HostName 
        { 
            get
            {
                return _hostName;
            }
            set
            {
                var changed = value != _hostName;
                if (changed)
                {
                    _hostName = value;
                    RaiseParameterChangedEvent();
                }
            }
        }
        protected int _port;
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                var changed = (value != _port);
                if (changed)
                {
                    _port = value;
                    RaiseParameterChangedEvent();
                }
            }
        }

        protected const Protocols _defaultProtocol = Protocols.NamedPipe;
        protected const string _defaultIdentifier = "GIPC";

        public GIPCBase()
        {
            _hostName = DefaultHostName;
            _port = 8000;
        }

        public string GenerateEndPointAddress()
        {
            switch (this.Protocol)
            {
                case GIPCServer.Protocols.NamedPipe:
                    return String.Join("/", new string[] { this.GetNamedPipeProtocolAndHost(), this.GenerateEndPointName() });
                case Protocols.NetTcp:
                    return String.Join("/", new string[] { this.GetNetTcpProtocolAndHost(), this.GenerateEndPointName() });
                default: 
                    return null;
            }
        }

        protected string GetNetTcpProtocolAndHost()
        {
            return String.Join("", new[] { NetTcpProtocol, HostName, ":", Port.ToString() });
        }

        protected string GetNamedPipeProtocolAndHost()
        {
            return NamedPipeProtocol + HostName;
        }

        public string GenerateEndPointName()
        {
            return string.Join("/", new string[] { AddressBaseName, this.Identifier });
        }

        protected virtual string SanitiseIdentifier(string id)
        {
            return HttpUtility.UrlEncode(id);
        }

        protected NetTcpBinding CreateNetTcpBinding()
        {
            return new NetTcpBinding() 
            { 
                Security = new NetTcpSecurity() 
                { 
                    Mode = SecurityMode.None
                },
                MaxReceivedMessageSize = GetConfiguredMaxAllowedMessageSize()
            };
        }

        private int GetConfiguredMaxAllowedMessageSize()
        {
            var sizeInMb = MaxMessageSizeInMB <= 0 ? 25 : MaxMessageSizeInMB;
            return (int)(sizeInMb * 1024 * 1204);
        }
    }

    public class ParameterChangedEventArgs
    {
        public string ChangedParameter { get; protected set; }
        public ParameterChangedEventArgs(string changedParameter)
        {
            ChangedParameter = changedParameter;
        }
    }
}
