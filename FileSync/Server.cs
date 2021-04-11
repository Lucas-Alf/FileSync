using System;
using System.Net;
using System.Net.Sockets;

namespace FileSync
{
    public class Server
    {
        private static string Host;
        private static int Port;
        private static string PathToSync;

        private static TcpClient Client;
        private static TcpListener TcpServer;

        public Server(string host, int port, string path)
        {
            Host = host;
            Port = port;
            PathToSync = path;
        }

        public void Start()
        {
            TcpServer = new TcpListener(IPAddress.Parse(Host), Port);
            try
            {
                TcpServer.Start();
                Client = TcpServer.AcceptTcpClient();
                Console.WriteLine("-> Client connected");
                new SyncDirectory(PathToSync, Client).Start();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                TcpServer.Stop();
            }
        }
    }
}
