using System.Net.Sockets;

namespace FileSync
{
    class Client
    {
        private static TcpClient TcpClient;
        private static string Host;
        private static int Port;
        private static string PathToSync;

        public Client(string host, int port, string pathToSync)
        {
            Host = host;
            Port = port;
            PathToSync = pathToSync;
        }

        public void Start()
        {
            TcpClient = new TcpClient(Host, Port);
            new SyncDirectory(PathToSync, TcpClient).Start();
        }
    }
}
