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
        static FileSystemConfig config;
        static void RunServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();
            Server server = new Server(config, listener);
            ThreadStart ts = server.Start;
            serverThread = new Thread(ts);
            serverThread.Start();
        }

        static void SendMessage()
        {
            Client client = new Client(config, "127.0.0.1", listenPort);
            ThreadStart ts = client.SendFile;
            clientThread = new Thread(ts);
            clientThread.Start();
        }

        static void LoadConfig()
        {
            string configText = File.ReadAllText("config.json");
            config = JsonConvert.DeserializeObject<FileSystemConfig>(configText);
        }

        static void Main(string[] args)
        {
            LoadConfig();

            List<Watcher> watchers = new List<Watcher>();
            watchers.Add(new Watcher());
            RunServer();

            Console.WriteLine("Listening for changes to file system");
            foreach(Watcher w in watchers)
            {
                w.FileChangeDetected += HandleFileChange;
                w.Start();
            }

            SendMessage();

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

        private static void HandleFileChange(object sender, FileSystemEventArgs e)
        {
            using(BinaryReader reader = new BinaryReader(File.OpenRead(e.FullPath)))
            {
            }
            Console.WriteLine("detected {0} at {1}", e.ChangeType.ToString(), e.Name);
        }
    }


        
}
