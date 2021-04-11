using System;
using System.IO;

namespace FileSync
{
    public class Document
    {
        public string Client { get; set; }
        public string Name { get; set; }
        public string OldName { get; set; }
        public WatcherChangeTypes Type { get; set; }
        public DateTime Modified { get; set; }
        public string Checksum { get; set; }
        public byte[] Content { get; set; }
    }
}
