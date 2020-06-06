using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Config
{
    public class Connection
    {
        public string Nickname { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string AccessKey { get; set; }
    }
}
