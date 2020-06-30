using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSync.Library.Config
{
    public class FileSyncShare
    {
        [JsonIgnore]
        public string Path { get; set; }
        public List<Connection> Connections { get; set; }

        public Connection GetConnection(string address)
        {
            return Connections.Where(c => c.Address == address).FirstOrDefault();
        }

        public FileSyncShare()
        {
            Connections = new List<Connection>();
        }
    }
}
