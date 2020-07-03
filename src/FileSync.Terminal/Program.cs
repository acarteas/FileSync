using FileSync.Library.Config;
using FileSync.Library.FileSystem;
using FileSync.Library.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Threading;

namespace FileSync.Terminal
{
    class Program
    {
        static Thread serverThread = null;
        static Thread clientThread = null;
        static int listenPort = 13000;
        static FileSyncConfig config;
        

        static void LoadConfig()
        {
            string configText = File.ReadAllText("config.json");
            config = JsonConvert.DeserializeObject<FileSyncConfig>(configText);
        }


        static void Main(string[] args)
        {
            LoadConfig();

            List<Watcher> watchers = new List<Watcher>();
            watchers.Add(new Watcher());

            Console.WriteLine("Listening for changes to file system");
            foreach(Watcher w in watchers)
            {
                w.FileChangeDetected += HandleFileChange;
                w.Start();
            }


            //loop for as long as at least on watcher is active
            bool keepGoing = true;
            while(keepGoing == true)
            {
                keepGoing = false;
                foreach(Watcher w in watchers)
                {
                    if(w.RunningThread.IsAlive == true)
                    {
                        keepGoing = true;
                    }
                }
            }
            Console.WriteLine("Ending program");
        }

        private static void HandleFileChange(object sender, FsFileSystemEventArgs e)
        {
            RenamedEventArgs renamedArgs = e.BaseArgs as RenamedEventArgs;

            //file or directory
            bool isDirectory = Directory.Exists(e.BaseArgs.FullPath);
            //using(BinaryReader reader = new BinaryReader(File.OpenRead(e.FullPath)))
            //{
            //}
            Console.WriteLine("detected {0} at {1}", e.BaseArgs.ChangeType.ToString(), e.BaseArgs.Name);
        }
    }


        
}
