﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Testing
{
    public class Helpers
    {
        /*
        public static List<FileSyncConfig> GenerateServerConfig()
        {
            string workingDirectory = Directory.GetCurrentDirectory();
            string server1Path = Path.Combine(workingDirectory, "server1");
            string server2Path = Path.Combine(workingDirectory, "server2");
            string server1Key = FileSyncConfig.GenerateAccessKey();
            string server2Key = FileSyncConfig.GenerateAccessKey();
            if (Directory.Exists(server1Path) == false)
            {
                Directory.CreateDirectory(server1Path);
            }
            if (Directory.Exists(server2Path) == false)
            {
                Directory.CreateDirectory(server2Path);
            }

            FileSyncConfig server1Config = new FileSyncConfig();
            server1Config.LocalListenPort = 13001;
            server1Config.ServerThreadPoolCount = 1;

            Connection localConnection = new Connection()
            {
                LocalAccessKey = server1Key,
                RemoteAccessKey = server2Key,
                Address = "127.0.0.1",
                DirectoriesToSync = new List<string>(new string[] { "*" }),
                Nickname = "Sever 2",
                Port = 13002,
                LocalSyncPath = server1Path
            };
            server1Config.Shares.Add(server1Path, new FileSyncShare() { Path = server1Path });
            server1Config.Shares[server1Path].Connections.Add(localConnection);

            FileSyncConfig server2Config = new FileSyncConfig();
            server2Config.LocalListenPort = 13002;
            server2Config.ServerThreadPoolCount = 1;

            localConnection = new Connection()
            {
                LocalAccessKey = server2Key,
                RemoteAccessKey = server1Key,
                Address = "127.0.0.1",
                DirectoriesToSync = new List<string>(new string[] { "*" }),
                Nickname = "Sever 1",
                Port = 13001,
                LocalSyncPath = server2Path
            };
            server2Config.Shares.Add(server2Path, new FileSyncShare() { Path = server2Path });
            server2Config.Shares[server2Path].Connections.Add(localConnection);
            List<FileSyncConfig> result = new List<FileSyncConfig>();
            result.Add(server1Config);
            result.Add(server2Config);
            return result;
        }
        */
    }
}
