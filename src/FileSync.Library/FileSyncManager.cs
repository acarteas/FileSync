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
        public bool IsProcessingFiles
        {
            get
            {
                return _sendingFiles.Count != 0 || _receivingFiles.Count != 0;
            }
        }
        private Dictionary<string, int> _sendingFiles = new Dictionary<string, int>();
        private Dictionary<string, int> _receivingFiles = new Dictionary<string, int>();
        private ILogger _logger = null;

        public FileSyncManager(FileSyncConfig config, Connection connection, ILogger logger)
        {
            ServerThreads = new List<Thread>();
            _logger = logger;
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
            for (int i = 0; i < Config.ServerThreadPoolCount; i++)
            {
                Server server = new Server(Config, listener, _logger);

                //We listen to server events so that received file changes will not trigger
                //a send event from the Client 
                server.ReceiveBegin += ServerReceiveStart;
                server.ReceiveEnd += ServerReceiveComplete;
                ThreadStart ts = server.Start;
                ServerThreads.Add(new Thread(ts));
                ServerThreads[ServerThreads.Count - 1].Start();
            }
        }

        private void ServerReceiveComplete(object sender, ServerEventArgs e)
        {
            if (_receivingFiles.ContainsKey(e.FileData.Path))
            {
                _receivingFiles.Remove(e.FileData.Path);
            }
        }

        private void ServerReceiveStart(object sender, ServerEventArgs e)
        {
            //TODO: verify that local file information matches file information passed via ServerEventArgs param
            if (_receivingFiles.ContainsKey(e.FileData.Path) == false)
            {
                _receivingFiles.Add(e.FileData.Path, 1);
            }
        }

        private void SyncFile(FileMetaData data)
        {
            //prevent multiple sends and from sending a file that we are presently receiving
            if (_sendingFiles.ContainsKey(data.Path) == false && _receivingFiles.ContainsKey(data.Path) == false)
            {
                _sendingFiles.Add(data.Path, 1);

                Client client = new Client(ActiveConnection, _logger);
                client.DataToSend = data;
                client.SendComplete += ClientSendComplete;
                ThreadStart ts = client.SendFile;
                ClientThread = new Thread(ts);
                ClientThread.Start();
            }
        }

        private void ClientSendComplete(object sender, ClientEventArgs e)
        {
            //unlock file
            _sendingFiles.Remove(e.FileName);

            if (e.WasSuccessful == false)
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
                if (renamed != null)
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
