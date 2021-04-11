using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace FileSync
{
    class Program
    {
        private static string Host { get; set; }
        private static int Port { get; set; }
        private static string PathToSync { get; set; }
        private static string Mode { get; set; }

        private static Dictionary<string, DateTime> lastWriteDate = new Dictionary<string, DateTime>();

        static void Main(string[] args)
        {
            Console.WriteLine(@"
 ███████████  ███  ████            █████████                                
░░███░░░░░░█ ░░░  ░░███           ███░░░░░███                               
 ░███   █ ░  ████  ░███   ██████ ░███    ░░░  █████ ████ ████████    ██████ 
 ░███████   ░░███  ░███  ███░░███░░█████████ ░░███ ░███ ░░███░░███  ███░░███
 ░███░░░█    ░███  ░███ ░███████  ░░░░░░░░███ ░███ ░███  ░███ ░███ ░███ ░░░ 
 ░███  ░     ░███  ░███ ░███░░░   ███    ░███ ░███ ░███  ░███ ░███ ░███  ███
 █████       █████ █████░░██████ ░░█████████  ░░███████  ████ █████░░██████ 
░░░░░       ░░░░░ ░░░░░  ░░░░░░   ░░░░░░░░░    ░░░░░███ ░░░░ ░░░░░  ░░░░░░  
                                               ███ ░███                     
                                              ░░██████                      
                                               ░░░░░░                       ");


            try
            {
                Mode = args[0];
                Host = args[1];
                Port = Convert.ToInt32(args[2]);
                PathToSync = args[3];
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> Invalid initializer");
                Console.ResetColor();
                Environment.Exit(0);
            }


            switch (Mode)
            {
                case "-s":
                    Console.Write("-> File Sync Server: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("ON");
                    Console.ResetColor();
                    Console.WriteLine();
                    ReceiverListener.Listner(Host, Port, PathToSync);
                    break;
                case "-c":
                    Console.Write("-> File Sync Client: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("ON");
                    Console.ResetColor();
                    Console.WriteLine();

                    var files = Directory.GetFiles(PathToSync);
                    foreach (var file in files)
                    {
                        var document = new Document
                        {
                            Client = GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                            Name = Path.GetFileName(file),
                            Modified = File.GetLastWriteTime(file),
                            Checksum = CheckSum(file),
                            Content = File.ReadAllBytes(file)
                        };
                        Client.Send(Host, Port, document).Wait();
                    }
                    WatchDirectory();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("-> Invalid initializer");
                    Console.ResetColor();
                    Environment.Exit(0);
                    break;
            }
        }

        private static string CheckSum(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static void WatchDirectory()
        {

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
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
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

            Client.Send(Host, Port, new Document
            {
                Client = GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                Name = Path.GetFileName(filePath),
                OldName = e.OldName,
                Type = e.ChangeType,
            }).Wait();
        }


        private static void OnChanged(object source, FileSystemEventArgs e)
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
                Client.Send(Host, Port, new Document
                {
                    Client = GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                    Name = Path.GetFileName(filePath),
                    Type = e.ChangeType,
                }).Wait();
            }
            else
            {
                Client.Send(Host, Port, new Document
                {
                    Client = GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString()),
                    Name = Path.GetFileName(filePath),
                    Type = e.ChangeType,
                    Modified = File.GetLastWriteTime(filePath),
                    Checksum = CheckSum(filePath),
                    Content = File.ReadAllBytes(filePath)
                }).Wait();
            }
        }

        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = MD5.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}
