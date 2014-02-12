using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace GIPC
{
    [ServiceContract]
    interface IGIPCServer
    {
        [OperationContract]
        string[] SendMessage(string message); // send a string message to the server, get a string back
        // TODO: implement XML and binary message interfaces
    }
}
