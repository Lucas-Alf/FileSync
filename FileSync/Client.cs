using Newtonsoft.Json;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FileSync
{
    class Client
    {
        public static async Task Send(string host, int port, Document document)
        {
            var message = JsonConvert.SerializeObject(document);

            using (TcpClient client = new TcpClient(host, port))
            {
                byte[] data = System.Text.Encoding.Unicode.GetBytes(message);

                NetworkStream stream = client.GetStream();

                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine("{0} - {1}: {2}", DateTime.Now.ToUniversalTime(), document.Type.ToString(), document.Name);
            }
        }
    }
}
