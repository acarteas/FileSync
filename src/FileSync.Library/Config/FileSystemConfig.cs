using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FileSync.Library.Config
{
    public class FileSystemConfig
    {
        public int LocalListenPort { get; set; }
        public string LocalAccessKey { get; set; }
        public Dictionary<string, Connection> RemoteConnections { get; set; }
        public int ServerThreadPoolCount { get; set; }
        public FileSystemConfig()
        {
            RemoteConnections = new Dictionary<string, Connection>();
        }

        public static string GenerateAccessKey()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }
    }
}
