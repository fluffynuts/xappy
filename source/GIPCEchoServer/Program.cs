using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GIPC;

namespace GIPCEchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var id = "echo";
            string hostName = GetHostNameFrom(args);
            var port = GetPortFrom(args);
            using (var server = new GIPCServer(GIPCBase.Protocols.NetTcp, id) { HostName = hostName ?? Dns.GetHostName(), Port = port ?? 8080 })
            {
                server.OnMessageReceived += (msg) =>
                {
                    Console.WriteLine("Got a client connection with message: " + msg);
                    return "You said: " + msg;
                };
                try
                {
                    server.Start();
                    Console.WriteLine("Listening for incoming requests ({0}:{1})...", server.HostName, server.Port);
                    while (true) Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Join("\n", new[]
                        {
                            "Unable to start server:",
                            ex.Message,
                            ex.StackTrace
                        }));
                    throw;
                }
            }
        }

        private static int? GetPortFrom(string[] args)
        {
            var hostName = GetHostNamePartFrom(args);
            if (hostName == null) return null;
            var hostParts = hostName.Split(':');
            if (hostParts.Length < 2) return null;
            int result;
            if (int.TryParse(hostParts[1], out result))
            {
                if (result < 1) return null;
                return result;
            }
            return null;
        }

        private static string GetHostNamePartFrom(string[] args)
        {
            if (args.Length == 0) return null;
            var parts = args[0].Split('/');
            if (parts.Length < 2) return parts[0];
            if (parts.Length < 3) return null;
            return parts[3];
        }

        private static string GetHostNameFrom(string[] args)
        {
            var hostName = GetHostNamePartFrom(args);
            if (hostName == null) return null;
            var hostParts = hostName.Split(':');
            return hostParts[0];
        }
    }
}
