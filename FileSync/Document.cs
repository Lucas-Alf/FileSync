using System;

namespace FileSync
{
    public class Document
    {
        public string Name { get; set; }
        public DateTime Modified { get; set; }
        public string Checksum { get; set; }
        public byte[] Content { get; set; }
    }
}
