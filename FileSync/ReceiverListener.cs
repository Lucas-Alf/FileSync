using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync
{
    public class ReceiverListener
    {
        public static void Listner(string host, int port, string path)
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
                                    received = Encoding.Unicode.GetString(ms.ToArray(), 0, (int)ms.Length);
                                    var document = JsonConvert.DeserializeObject<Document>(received);

                                    Console.WriteLine("{0} - {1}: {2}", DateTime.Now.ToUniversalTime(), document.Type.ToString(), document.Name);

                                    var newFilePath = $"{path}/{document.Client}/{document.Name}";

                                    if (!Directory.Exists($"{path}/{document.Client}"))
                                        Directory.CreateDirectory($"{path}/{document.Client}");

                                    if (document.Type == WatcherChangeTypes.Deleted)
                                    {
                                        if (File.Exists(newFilePath))
                                            File.Delete(newFilePath);
                                    }
                                    else if (document.Type == WatcherChangeTypes.Renamed)
                                    {
                                        File.Move($"{path}/{document.Client}/{document.OldName}", newFilePath);
                                    }
                                    else
                                    {
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
