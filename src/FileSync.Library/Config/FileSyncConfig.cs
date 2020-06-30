﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FileSync.Library.Config
{
    public class FileSyncConfig
    {
        public int LocalListenPort { get; set; }
        public int ServerThreadPoolCount { get; set; }
        public Dictionary<string, FileSyncShare> Shares { get; set; }

        /// <summary>
        /// The maximum number of times that the Sync Manager will attempt to 
        /// send a file update to another machine
        /// </summary>
        public int MaxSendAttempts { get; set; }
        public FileSyncConfig()
        {
            Shares = new Dictionary<string, FileSyncShare>();
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
