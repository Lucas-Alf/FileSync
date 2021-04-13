using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace FileSync
{
    public class Document
    {
        public bool ResendAll { get; set; }
        public string Client { get; set; } = CryptTools.GetHashString(NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).First().GetPhysicalAddress().ToString());
        public string Name { get; set; }
        public string OldName { get; set; }
        public WatcherChangeTypes Type { get; set; }
        public DateTime Modified { get; set; }
        public string Checksum { get; set; }
        public byte[] Content { get; set; }
    }
}
