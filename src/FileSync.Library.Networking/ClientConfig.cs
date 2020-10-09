using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Networking
{
    public class ClientConfig
    {
        public string AuthKey { get; set; }
        public string ShareName { get; set; }
        public string ServerIpAddress { get; set; }
        public int ServerPort { get; set; }
    }
}
