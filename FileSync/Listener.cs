using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync
{
    public class Listener
    {
        public static void Start(string host, int port, string path)
        {
            do
            {
                TcpListener server = new TcpListener(IPAddress.Parse(host), port);
                try
                {

                    server.Start();

                    while (true)
                    {
                        using (TcpClient client = server.AcceptTcpClient())
                        {
                            string received;
                            using (NetworkStream stream = client.GetStream())
                            {

                                byte[] data = new byte[1000000];
                                using (MemoryStream ms = new MemoryStream())
                                {

                                    int numBytesRead;
                                    while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                                    {
                                        ms.Write(data, 0, numBytesRead);
                                    }
                                    received = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                                    var document = JsonConvert.DeserializeObject<Document>(received);

                                    Console.WriteLine(String.Format("Received: " + document.Name));

                                    var newFilePath = $"{path}/{document.Name}";
                                    if (!File.Exists(newFilePath))
                                    {
                                        File.WriteAllBytes(newFilePath, document.Content);
                                    }
                                    else
                                    {
                                        var currentModified = File.GetLastWriteTime(newFilePath);
                                        if (currentModified < document.Modified)
                                        {
                                            File.WriteAllBytes(newFilePath, document.Content);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                finally
                {
                    server.Stop();
                }
            } while (true);
        }
    }
}
