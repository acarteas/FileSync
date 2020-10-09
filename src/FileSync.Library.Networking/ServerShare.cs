using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Networking
{
    public class ServerShare
    {
        [JsonIgnore]
        public string Name { get; set; }
        public string AbsolutePath { get; set; }

        /// <summary>
        /// Should be in the format [API Key], [IP Address] 
        /// </summary>
        public Dictionary<string, string> Clients { get; set; }

        public ServerShare()
        {
            Clients = new Dictionary<string, string>();
        }
    }
}
