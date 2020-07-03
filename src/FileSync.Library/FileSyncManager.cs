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
        public FileSyncShare Share { get; set; }
        protected List<Server> ServerThreads { get; set; }
        protected Thread SendThread { get; set; }
        TcpListener _listener;
        public bool IsProcessingFiles
        {
            get
            {
                return _sendQueue.IsEmpty() == false || _receivingFiles.Count != 0;
            }
        }
        private Dictionary<string, Server> _receivingFiles = new Dictionary<string, Server>();
        private ILogger _logger = null;
        public event EventHandler<ServerEventArgs> FileReceived = delegate { };
        private SendQueue _sendQueue = new SendQueue();

        public FileSyncManager(FileSyncConfig config, FileSyncShare share, ILogger logger)
        {
            ServerThreads = new List<Server>();
            _logger = logger;
            Config = config;
            Share = share;
            Watcher = new Watcher(Share.Path);
            Watcher.FileChangeDetected += WatchedFileChanged;
        }

        private void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, Config.LocalListenPort);
            _listener.Start();

            //spawn appropriate number of server threads
            for (int i = 0; i < Config.ServerThreadPoolCount; i++)
            {
                Server server = new Server(Share, _listener, _logger);
                ServerThreads.Add(server);

                //We listen to server events so that received file changes will not trigger
                //a send event from the Client 
                server.ReceiveBegin += ServerReceiveStart;
                server.ReceiveEnd += ServerReceiveComplete;
                server.Start();
            }
        }

        private void ServerReceiveComplete(object sender, ServerEventArgs e)
        {
            lock(_receivingFiles)
            {
                if (_receivingFiles.ContainsKey(e.FileData.Path))
                {
                    _receivingFiles.Remove(e.FileData.Path);
                    e.FullLocalPath = Path.Join(Share.Path, e.FileData.Path);
                    FileReceived(this, e);
                }
                if (_receivingFiles.ContainsKey(e.FileData.OldPath))
                {
                    _receivingFiles.Remove(e.FileData.OldPath);
                }
            }
        }

        private void ServerReceiveStart(object sender, ServerEventArgs e)
        {
            lock(_receivingFiles)
            {
                if (e.FileData.Path != null && e.FileData.Path.Length > 0 && _receivingFiles.ContainsKey(e.FileData.Path) == false)
                {
                    Server server = sender as Server;
                    if (server != null)
                    {
                        _receivingFiles.Add(e.FileData.Path, server);
                    }
                }

                //rename ops cause issues as we are essentially editing 2 files: the old name and the new name.  To avoid conflicts,
                //add old file name to the list of files that we are receiving
                if (e.FileData.OperationType == WatcherChangeTypes.Renamed && e.FileData.OldPath != null && e.FileData.OldPath.Length > 0 && _receivingFiles.ContainsKey(e.FileData.OldPath) == false)
                {
                    Server server = sender as Server;
                    if (server != null)
                    {
                        _receivingFiles.Add(e.FileData.OldPath, server);
                    }
                }
            }
        }

        private void WatchedFileChanged(object sender, FsFileSystemEventArgs e)
        {
            FileInfo info = new FileInfo(e.BaseArgs.FullPath);
            string formattedRegularPath = Path.GetFullPath(Share.Path);
            string relativePath = info.FullName.Substring(formattedRegularPath.Length);

            FileMetaData metaData = new FileMetaData()
            {
                LastWriteTimeUtc = info.LastWriteTimeUtc,
                LastAccessTimeUtc = info.LastAccessTimeUtc,
                CreateTimeUtc = info.CreationTimeUtc,
                OperationType = e.BaseArgs.ChangeType,
                Path = relativePath
            };
            if (e.BaseArgs.ChangeType == WatcherChangeTypes.Renamed)
            {
                RenamedEventArgs renamed = e.BaseArgs as RenamedEventArgs;
                if (renamed != null)
                {
                    string oldRelativePath = renamed.OldFullPath.Substring(formattedRegularPath.Length);
                    metaData.OldPath = oldRelativePath;
                }
            }

            //don't queue a file that we are currently receiving
            if (_receivingFiles.ContainsKey(metaData.Path) == false)
            {
                lock (_sendQueue)
                {
                    _sendQueue.Enqueue(metaData);
                }
            }
            else
            {
                _logger.Log("Ignoring change on file {0}", e.BaseArgs.FullPath);
            }
        }

        private void SendLoop()
        {
            while (true)
            {
                if (_sendQueue.IsEmpty() == false)
                {
                    var nextFile = _sendQueue.Front();

                    // don't send a file that we are currently receiving.  Instead,
                    //wait for file to complete before sending.
                    if (_receivingFiles.ContainsKey(nextFile.Path) == false)
                    {
                        int successCount = 0;
                        foreach (var connection in Share.Connections)
                        {
                            Client client = new Client(connection, _logger);
                            var result = client.SendFile(nextFile);
                            if(result.WasSuccessful == true)
                            {
                                successCount++;
                            }
                        }

                        //open question: is success only when all servers receive the new data?
                        if (successCount == Share.Connections.Count)
                        {
                            lock (_sendQueue)
                            {
                                _sendQueue.Dequeue();
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                    
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public void Start()
        {
            //begin listening for network connections
            StartServer();

            //begin listening for changes to file system
            Watcher.Start();

            //begin send thread
            ThreadStart ts = SendLoop;
            SendThread = new Thread(ts);
            SendThread.Start();
        }

        public void Stop()
        {
            Watcher.Stop();
            _listener.Stop();
            foreach (var server in ServerThreads)
            {
                server.Stop();
            }
        }
    }
}
