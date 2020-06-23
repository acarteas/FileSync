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

        protected List<Thread> ReceiveThreads { get; set; }
        protected Thread SendThread { get; set; }
        public Connection ActiveConnection { get; protected set; }
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

        public FileSyncManager(FileSyncConfig config, Connection connection, ILogger logger)
        {
            ReceiveThreads = new List<Thread>();
            _logger = logger;
            Config = config;
            ActiveConnection = connection;
            Watcher = new Watcher(connection.LocalSyncPath);
            Watcher.FileChangeDetected += WatchedFileChanged;
        }

        private void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, Config.LocalListenPort);
            _listener.Start();

            //spawn appropriate number of server threads
            for (int i = 0; i < Config.ServerThreadPoolCount; i++)
            {
                Server server = new Server(Config, _listener, _logger);

                //We listen to server events so that received file changes will not trigger
                //a send event from the Client 
                server.ReceiveBegin += ServerReceiveStart;
                server.ReceiveEnd += ServerReceiveComplete;
                ThreadStart ts = server.Start;
                ReceiveThreads.Add(new Thread(ts));
                ReceiveThreads[ReceiveThreads.Count - 1].Start();
            }
        }

        private void ServerReceiveComplete(object sender, ServerEventArgs e)
        {
            if (_receivingFiles.ContainsKey(e.FileData.Path))
            {
                _receivingFiles.Remove(e.FileData.Path);
                e.FullLocalPath = Path.Join(ActiveConnection.LocalSyncPath, e.FileData.Path);
                FileReceived(this, e);
            }
            if (_receivingFiles.ContainsKey(e.FileData.OldPath))
            {
                _receivingFiles.Remove(e.FileData.OldPath);
            }
        }

        private void ServerReceiveStart(object sender, ServerEventArgs e)
        {
            //AC: getting random null exception.  Cannot figure out why
            try
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
            catch (Exception ex)
            {
                _logger.Log(LogPriority.High, "Exception in ServerReceiveStart: {0}", ex.Message);
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

            //don't queue a file that we are currently receiving
            if (_receivingFiles.ContainsKey(metaData.Path) == false)
            {
                lock (_sendQueue)
                {
                    _sendQueue.Enqueue(metaData);
                }
            }
        }

        private void SendLoop()
        {
            while (true)
            {
                if (_sendQueue.IsEmpty() == false)
                {
                    Client client = new Client(ActiveConnection, _logger);
                    var nextFile = _sendQueue.Front();

                    // don't send a file that we are currently receiving.  Instead,
                    //wait for file to complete before sending.
                    if (_receivingFiles.ContainsKey(nextFile.Path) == false)
                    {
                        var result = client.SendFile(nextFile);
                        if (result.WasSuccessful == true)
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
            foreach (var thread in ReceiveThreads)
            {
                //TODO: kill server threads (Abort not supported in .NET CORE)
            }
        }
    }
}
