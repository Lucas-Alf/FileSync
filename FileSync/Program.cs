using System;
using System.Collections.Generic;

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
                    new Server(Host, Port, PathToSync).Start();
                    break;
                case "-c":
                    Console.Write("-> File Sync Client: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("ON");
                    Console.ResetColor();
                    Console.WriteLine();
                    new Client(Host, Port, PathToSync).Start();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("-> Invalid initializer");
                    Console.ResetColor();
                    Environment.Exit(0);
                    break;

            }
        }
    }
}
