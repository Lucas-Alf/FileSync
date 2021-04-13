using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
            Console.WriteLine("Write \"pause\" or \"resume\" to start/stop the sync.");

            CheckConnetion().Start();

            var cancelationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancelationTokenSource.Token;

            var syncDir = SyncDir(cancellationToken);
            syncDir.Start();

            var listen = Listen(cancellationToken);
            listen.Start();

            var running = true;
            while (true)
            {
                var userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "pause":
                        if (!running)
                        {
                            Console.WriteLine("-> The sync is already paused.");
                        }
                        else
                        {
                            cancelationTokenSource.Cancel();
                            running = false;
                        }
                        break;
                    case "resume":
                        if (running)
                        {
                            Console.WriteLine("-> The sync is already running.");
                        }
                        else
                        {
                            cancelationTokenSource = new CancellationTokenSource();
                            cancellationToken = cancelationTokenSource.Token;

                            listen = Listen(cancellationToken);
                            listen.Start();

                            Send(new Document
                            {
                                ResendAll = true,
                                Name = "Resend All"
                            });

                            syncDir = SyncDir(cancellationToken);
                            syncDir.Start();

                            running = true;
                        }
                        break;
                    default:
                        Console.WriteLine("-> Invalid command.");
                        break;
                }
            }
        }

        private static Task CheckConnetion()
        {
            return new Task(() =>
            {
                var state = Convert.ToBoolean(Client.Connected.ToString());
                while (true)
                {
                    if (state != Client.Connected)
                    {
                        Console.WriteLine($"-> Connection changed to: {(Client.Connected ? "Connected" : "Disconected")}");
                        state = Convert.ToBoolean(Client.Connected.ToString());
                    }
                    Thread.Sleep(500);
                }
            });
        }

        private Task SyncDir(CancellationToken cancellationToken)
        {
            return new Task(() =>
            {
                SendAll();

                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = PathToSync
                };
                Console.WriteLine("-> Start watching directory.");
                watcher.Created += new FileSystemEventHandler(OnChanged);
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Deleted += new FileSystemEventHandler(OnChanged);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);
                watcher.EnableRaisingEvents = true;


                while (Client.Connected)
                {

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("-> Stop watching directory.");
                        watcher.EnableRaisingEvents = false;
                        break;
                    }
                    else
                    {
                        Thread.Sleep(200);
                    }
                }
            }, cancellationToken);
        }

        private void SendAll()
        {
            var files = Directory.GetFiles(PathToSync);
            foreach (var file in files)
            {
                var content = ReadFile(file);
                var document = new Document
                {
                    Name = Path.GetFileName(file),
                    Modified = File.GetLastWriteTime(file),
                    Checksum = CryptTools.GetHashString(content),
                    Content = content
                };
                Send(document);
            }
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
                        Name = Path.GetFileName(filePath),
                        Type = e.ChangeType,
                    });
                }
                else
                {
                    var content = ReadFile(filePath);
                    Send(new Document
                    {
                        Name = Path.GetFileName(filePath),
                        Type = e.ChangeType,
                        Modified = File.GetLastWriteTime(filePath),
                        Checksum = CryptTools.GetHashString(content),
                        Content = content
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("File timeout"))
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


        public Task Listen(CancellationToken cancellationToken)
        {
            return new Task(() =>
            {
                Console.WriteLine("-> Start Listening.");
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("-> Stop listening.");
                        break;
                    }
                    else
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

                                List<Document> documents = new List<Document>();

                                if (received.Where(x => x == '}').Count() > 1)
                                {
                                    Regex regex = new Regex(@"(\})(\{)", RegexOptions.Multiline);
                                    received = regex.Replace(received, @"$1,$2");
                                    received = $"[{received}]";
                                    documents.AddRange(JsonConvert.DeserializeObject<IList<Document>>(received));
                                }
                                else
                                {
                                    documents.Add(JsonConvert.DeserializeObject<Document>(received));
                                }

                                foreach (var document in documents)
                                {
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

                                    if (document.ResendAll)
                                    {
                                        SendAll();
                                    }
                                    else
                                    {
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
                                            if (File.Exists($"{PathToSync}/{document.OldName}"))
                                            {
                                                File.Move($"{PathToSync}/{document.OldName}", newFilePath);
                                                CanSend = false;
                                            }
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
                        }
                        Thread.Sleep(1000);
                    }
                }
            }, cancellationToken);

        }
        private byte[] ReadFile(string path)
        {
            using (var fs = WaitForFile(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            for (int numTries = 0; numTries < 25; numTries++)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(50);
                }
            }

            throw new Exception("File timeout");
        }
    }
}
