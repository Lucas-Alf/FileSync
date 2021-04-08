using System;
using System.IO;
using System.Security.Cryptography;

namespace FileSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = args[1];
            var port = Convert.ToInt32(args[2]);
            var path = args[3];
            if (args[0] == "-s")
            {
                Console.WriteLine("FILE SYNC - SERVER");
                Listener.Start(host, port, path);
            }
            else
            {
                Console.WriteLine("FILE SYNC - CLIENT");

                var files = Directory.GetFiles(path);

                foreach (var file in files)
                {
                    var document = new Document
                    {
                        Name = Path.GetFileName(file),
                        Modified = File.GetLastWriteTime(file),
                        Checksum = CheckSum(file),
                        Content = File.ReadAllBytes(file)
                    };
                    Client.Send(host, port, document).Wait();
                }
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
    }
}
