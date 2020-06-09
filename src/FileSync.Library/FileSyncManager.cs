using FileSync.Library.Config;
using FileSync.Library.FileSystem;
using FileSync.Library.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileSync.Library
{
    public class FileSyncManager
    {
        protected Watcher Watcher { get; set; }
        public FileSystemConfig Config { get; set; }

        protected List<Thread> ServerThreads { get; set; }
        protected Thread ClientThread { get; set; }
        protected Connection ActiveConnection { get; set; }
        public bool IsSendingFile { get; private set; }

        public FileSyncManager(FileSystemConfig config, Connection connection)
        {
            Config = config;
            ActiveConnection = connection;
            Watcher = new Watcher(connection.LocalSyncPath);
            Watcher.FileChangeDetected += WatchedFileChanged;
        }

        private void StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Config.LocalListenPort);
            listener.Start();

            //spawn appropriate number of server threads
            for(int i = 0; i < Config.ServerThreadPoolCount; i++)
            {
                Server server = new Server(Config, listener);
                ThreadStart ts = server.Start;
                ServerThreads.Add(new Thread(ts));
                ServerThreads[ServerThreads.Count - 1].Start();
            }
        }

        private void SyncFile()
        {
            Client client = new Client(Config, ActiveConnection.Address, Config.LocalListenPort);
            client.SendComplete += ClientSendComplete;
            ThreadStart ts = client.SendFile;
            ClientThread = new Thread(ts);
            ClientThread.Start();
            IsSendingFile = true;
        }

        private void ClientSendComplete(object sender, ClientEventArgs e)
        {
            IsSendingFile = false;
            if(e.WasSuccessful == false)
            {
                //TODO: failure to send file to server
            }
        }

        private void WatchedFileChanged(object sender, System.IO.FileSystemEventArgs e)
        {

        }

        public void Start()
        {
            //begin listening for network connections
            StartServer();

            //begin listening for changes to file system
            Watcher.Start();
        }
    }
}
