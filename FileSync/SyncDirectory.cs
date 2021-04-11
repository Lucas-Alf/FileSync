using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileSync
{
    public class SyncDirectory
    {
        private static Dictionary<string, DateTime> lastWriteDate = new Dictionary<string, DateTime>();
        private static string PathToSync { get; set; }
        private static TcpClient Client { get; set; }
        private static bool CanSend { get; set; } = true;

        public SyncDirectory(string pathToSync, TcpClient client)
        {
            PathToSync = pathToSync;
            Client = client;
        }

        public void Start()
        {
            var sync = SyncDir();
            sync.Start();
            sync.Wait();
        }

        private Task SyncDir()
        {
            return new Task(() =>
            {
                var listner = Listen();
                listner.Start();

                var files = Directory.GetFiles(PathToSync);
                foreach (var file in files)
                {
                    var content = File.ReadAllBytes(file);
                    var document = new Document
                    {
                        Client = CryptTools.GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                        Name = Path.GetFileName(file),
                        Modified = File.GetLastWriteTime(file),
                        Checksum = CryptTools.GetHashString(content),
                        Content = content
                    };
                    Send(document);
                }

                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = PathToSync;
                watcher.Created += new FileSystemEventHandler(OnChanged);
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Deleted += new FileSystemEventHandler(OnChanged);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);
                watcher.EnableRaisingEvents = true;
                while (true)
                {
                    watcher.WaitForChanged(changeType: WatcherChangeTypes.All);
                }
            });
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            string filePath = e.FullPath;
            DateTime writeDate = File.GetLastWriteTime(filePath);
            if (lastWriteDate.ContainsKey(filePath))
            {
                if (lastWriteDate[filePath] == writeDate)
                    return;
                lastWriteDate[filePath] = writeDate;
            }
            else
            {
                lastWriteDate.Add(filePath, writeDate);
            }

            Send(new Document
            {
                Client = CryptTools.GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                Name = Path.GetFileName(filePath),
                OldName = e.OldName,
                Type = e.ChangeType,
            });
        }


        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                string filePath = e.FullPath;
                DateTime writeDate = File.GetLastWriteTime(filePath);
                if (lastWriteDate.ContainsKey(filePath))
                {
                    if (lastWriteDate[filePath] == writeDate)
                        return;
                    lastWriteDate[filePath] = writeDate;
                }
                else
                {
                    lastWriteDate.Add(filePath, writeDate);
                }


                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    Send(new Document
                    {
                        Client = CryptTools.GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                        Name = Path.GetFileName(filePath),
                        Type = e.ChangeType,
                    });
                }
                else
                {
                    var content = File.ReadAllBytes(filePath);
                    Send(new Document
                    {
                        Client = CryptTools.GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                        Name = Path.GetFileName(filePath),
                        Type = e.ChangeType,
                        Modified = File.GetLastWriteTime(filePath),
                        Checksum = CryptTools.GetHashString(content),
                        Content = content
                    });
                }
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("used by another process"))
                {
                    string filePath = e.FullPath;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("{0} - {1}: {2}", DateTime.Now.ToUniversalTime(), Path.GetFileName(filePath), "Cannot open file because it is being used by another process.");
                    Console.ResetColor();
                }
            }
        }


        private void Send(Document document)
        {
            if (CanSend)
            {
                var message = JsonConvert.SerializeObject(document);

                byte[] data = Encoding.Unicode.GetBytes(message);

                NetworkStream stream = Client.GetStream();

                stream.Write(data, 0, data.Length);

                var syncType = "";
                if (document.Type.ToString() == "0")
                {
                    syncType = "send";
                }
                else
                {
                    syncType = document.Type.ToString();
                }

                Console.WriteLine("{0} - {1}: {2}", DateTime.Now.ToUniversalTime(), syncType, document.Name);
            }
            else
            {
                CanSend = true;
            }
        }


        public Task Listen()
        {
            return new Task(() =>
            {
                while (Client.Connected)
                {
                    string received;
                    NetworkStream stream = Client.GetStream();
                    while (stream.DataAvailable)
                    {
                        byte[] data = new byte[1000000];
                        using (MemoryStream ms = new MemoryStream())
                        {

                            int numBytesRead;
                            while (stream.DataAvailable && (numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                            {
                                ms.Write(data, 0, numBytesRead);
                            }
                            received = Encoding.Unicode.GetString(ms.ToArray(), 0, (int)ms.Length);
                            var document = JsonConvert.DeserializeObject<Document>(received);

                            var syncType = "";
                            if (document.Type.ToString() == "0")
                            {
                                syncType = "received";
                            }
                            else
                            {
                                syncType = document.Type.ToString();
                            }

                            Console.WriteLine("{0} - {1}: {2}", DateTime.Now.ToUniversalTime(), syncType, document.Name);

                            var newFilePath = $"{PathToSync}/{document.Name}";

                            if (document.Type == WatcherChangeTypes.Deleted)
                            {
                                if (File.Exists(newFilePath))
                                {
                                    File.Delete(newFilePath);
                                    CanSend = false;
                                }
                            }
                            else if (document.Type == WatcherChangeTypes.Renamed)
                            {
                                File.Move($"{PathToSync}/{document.OldName}", newFilePath);
                                CanSend = false;
                            }
                            else
                            {
                                if (CryptTools.GetHashString(document.Content) != document.Checksum)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("{0} - CHECKSUM FAIL: {1}", DateTime.Now.ToUniversalTime(), document.Name);
                                    Console.ResetColor();
                                }
                                else
                                {
                                    if (!File.Exists(newFilePath))
                                    {
                                        File.WriteAllBytes(newFilePath, document.Content);
                                        CanSend = false;
                                    }
                                    else
                                    {
                                        var currentModified = File.GetLastWriteTime(newFilePath);
                                        if (currentModified < document.Modified)
                                        {
                                            File.WriteAllBytes(newFilePath, document.Content);
                                            CanSend = false;
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
                Console.WriteLine("Disconnected.");
            });
        }
    }
}
