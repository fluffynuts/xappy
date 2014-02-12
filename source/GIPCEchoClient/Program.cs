using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GIPC;

namespace GIPCEchoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var badArgsException = new Exception("please specify where to connect ({host}:{port})");
            if (args.Length != 1) throw badArgsException;
            var parts = args[0].Split(':');
            if (parts.Length != 2) throw badArgsException;
            var host = parts[0];
            int port;
            if (!int.TryParse(parts[1], out port)) throw badArgsException;
            using (var client = new GIPCClient(GIPCBase.Protocols.NetTcp, "echo") { HostName = host, Port = port })
            {
                try { client.Connect(); }
                catch { Console.WriteLine("Unable to connect to GIPC echo server at {0}:{1}", host, port); }
                while (true)
                {
                    var toSend = Console.ReadLine();
                    var received = client.SendMessage(toSend);
                    Console.WriteLine(String.Join("\n", received));
                }
            }
        }
    }
}
