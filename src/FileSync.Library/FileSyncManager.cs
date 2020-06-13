using FileSync.Library.Config;
using FileSync.Library.FileSystem;
using FileSync.Library.Logging;
using FileSync.Library.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileSync.Library
{
    public class FileSyncManager
    {
        protected Watcher Watcher { get; set; }
        public FileSyncConfig Config { get; set; }

        protected List<Thread> ServerThreads { get; set; }
        protected Thread ClientThread { get; set; }
        protected Connection ActiveConnection { get; set; }
        public bool IsSendingFile { get; private set; }
        private Dictionary<string, int> _activeFiles = new Dictionary<string, int>();

        public FileSyncManager(FileSyncConfig config, Connection connection)
        {
            ServerThreads = new List<Thread>();
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
                Server server = new Server(Config, listener, new ConsoleLogger());
                ThreadStart ts = server.Start;
                ServerThreads.Add(new Thread(ts));
                ServerThreads[ServerThreads.Count - 1].Start();
            }
        }

        private void SyncFile(FileMetaData data)
        {
            //prevent multiple sends
            if(_activeFiles.ContainsKey(data.Path) == false)
            {
                _activeFiles[data.Path] = 1;

                Client client = new Client(ActiveConnection, new ConsoleLogger());
                client.DataToSend = data;
                client.SendComplete += ClientSendComplete;
                ThreadStart ts = client.SendFile;
                ClientThread = new Thread(ts);
                ClientThread.Start();
                IsSendingFile = true;
            }

            
        }

        private void ClientSendComplete(object sender, ClientEventArgs e)
        {
            IsSendingFile = false;

            //unlock file
            _activeFiles.Remove(e.FileName);

            if(e.WasSuccessful == false)
            {
                //TODO: failure to send file to server
            }
        }

        private void WatchedFileChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo info = new FileInfo(e.FullPath);
            string formattedRegularPath = Path.GetFullPath(ActiveConnection.LocalSyncPath);
            string relativePath = info.FullName.Substring(formattedRegularPath.Length);
            
            FileMetaData metaData = new FileMetaData()
            {
                LastWriteTimeUtc = info.LastWriteTimeUtc,
                LastAccessTimeUtc = info.LastAccessTimeUtc,
                CreateTimeUtc = info.CreationTimeUtc,
                OperationType = e.ChangeType,
                Path = relativePath
            };
            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                RenamedEventArgs renamed = e as RenamedEventArgs;
                if(renamed != null)
                {
                    string oldRelativePath = renamed.OldFullPath.Substring(formattedRegularPath.Length);
                    metaData.OldPath = oldRelativePath;
                }
            }

            //with metadata built, send to server
            SyncFile(metaData);
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
