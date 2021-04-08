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
                byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                NetworkStream stream = client.GetStream();

                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine("Send: {0}", document.Name);

                //int bytes = await stream.ReadAsync(data, 0, data.Length);
                //var responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                //Console.WriteLine("Received: {0}", responseData);
            }
        }
    }
}
