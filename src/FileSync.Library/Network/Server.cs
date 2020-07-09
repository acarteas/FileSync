using FileSync.Library.Config;
using FileSync.Library.Logging;
using FileSync.Library.Network.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Linq;
using System.Threading;
using System.Xml.XPath;

namespace FileSync.Library.Network
{
    //TODO: add option for SSL communication (tutorial at https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1)
    public class Server
    {
        private bool Run { get; set; }
        private FileSyncShare ShareConfig { get; set; }
        private int ServerId { get; set; }
        private bool ClientHasBeenValidated { get; set; }
        private FileMetaData FileMetaData { get; set; }
        private Thread ServerThread { get; set; }
        private static int _server_counter = 1;

        public event EventHandler<ServerEventArgs> ReceiveBegin = delegate { };
        public event EventHandler<ServerEventArgs> ReceiveEnd = delegate { };
        public ILogger Logger { get; set; }
        public TcpListener Listener { get; protected set; }
        public Server(FileSyncShare config, TcpListener listener, ILogger logger)
        {
            Run = true;
            ShareConfig = config;
            Listener = listener;
            ServerId = _server_counter;
            _server_counter++;
            Logger = logger;
        }

        public void Stop()
        {
            Run = false;
        }

        public void Start()
        {
            ThreadStart ts = ServerLoop;
            ServerThread = new Thread(ts);
            ServerThread.Start();
        }

        private void ServerLoop()
        {
            while (Run)
            {
                Logger.Log("Server #{0} waiting for connection...", ServerId);
                TcpClient client = null;
                try
                {
                    client = Listener.AcceptTcpClient();

#if DEBUG == false
                    //timeouts affect debugging when stepping through code
                    client.ReceiveTimeout = 5000;
#endif

                }
                catch (Exception ex)
                {
                    Logger.Log("Could not accept TCP client: {0}", ex.Message);
                    Run = false;
                    continue;
                }

                //reset server state
                ClientHasBeenValidated = false;
                FileMetaData = null;

                Logger.Log("Server #{0} accepting client: {1}", ServerId, client.Client.RemoteEndPoint);

                //reject connections not stored in our config
                string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                Connection activeConnection = ShareConfig.GetConnection(address);
                if (activeConnection != null)
                {
                    BufferedStream stream = new BufferedStream(client.GetStream());
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    try
                    {
                        IMessage result = ProcessData(activeConnection, reader);
                        while (result.MessageId != MessageIdentifier.Null)
                        {
                            writer.Write(result.ToBytes());
                            result = ProcessData(activeConnection, reader);
                        }

                        //send null message back to client to close connection
                        writer.Write(result.ToBytes());
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Server #{0} exception: {1}", ServerId, ex.Message);
                    }
                    finally
                    {
                        Logger.Log("Server #{0} done handling client", ServerId);
                        reader.Close();
                        writer.Close();
                    }
                }
            }
        }

        protected IMessage ProcessData(Connection connection, BinaryReader reader)
        {
            IMessage result = new NullMessage();
            IMessage currentMessage = MessageFactory.FromStream(reader);
            switch (currentMessage.MessageId)
            {
                case MessageIdentifier.FileChanged:
                    {
                        if (ClientHasBeenValidated == true)
                        {
                            FileChangedMessage message = currentMessage as FileChangedMessage;
                            string filePath = Path.Join(connection.LocalSyncPath, message.FileData.Path);
                            FileInfo localFile = new FileInfo(filePath);

                            //we only need the file data when a file on the client was changed and that file is newer than our local copy
                            if (message.FileData.OperationType == WatcherChangeTypes.Changed || message.FileData.OperationType == WatcherChangeTypes.Created)
                            {
                                if (localFile.Exists == false || localFile.LastWriteTimeUtc < message.FileData.LastWriteTimeUtc)
                                {
                                    FileMetaData = message.FileData;
                                    result = new FileRequestMessage(message.FileData);
                                }
                                else
                                {
                                    result = new NullMessage();
                                }
                            }
                            else
                            {
                                if (message.FileData.OperationType == WatcherChangeTypes.Renamed)
                                {
                                    string oldFilePath = Path.Join(connection.LocalSyncPath, message.FileData.OldPath);
                                    if (File.Exists(oldFilePath))
                                    {
                                        File.Move(oldFilePath, filePath);
                                    }
                                    else
                                    {
                                        //we're being informed about a file that isn't on our local file system.  Request file from client
                                        FileMetaData = message.FileData;
                                        result = new FileRequestMessage(message.FileData);
                                    }
                                }
                                if (message.FileData.OperationType == WatcherChangeTypes.Deleted)
                                {
                                    if (File.Exists(filePath))
                                    {
                                        File.Delete(filePath);
                                    }
                                }
                                result = new NullMessage();
                            }
                        }
                        break;
                    }

                case MessageIdentifier.FileData:
                    {
                        if (ClientHasBeenValidated == true && FileMetaData != null)
                        {
                            FileDataMessage message = currentMessage as FileDataMessage;
                            message.LocalPath = Path.Join(connection.LocalSyncPath, FileMetaData.Path);
                            ReceiveBegin(this, new ServerEventArgs() { FileData = FileMetaData, FullLocalPath = message.LocalPath });

                            try
                            {
                                message.WriteFileData();

                                //change last write to match client file
                                File.SetLastWriteTimeUtc(message.LocalPath, FileMetaData.LastWriteTimeUtc);
                                File.SetLastAccessTimeUtc(message.LocalPath, FileMetaData.LastAccessTimeUtc);
                                File.SetCreationTimeUtc(message.LocalPath, FileMetaData.CreateTimeUtc);
                                ReceiveEnd(this, new ServerEventArgs() { FileData = FileMetaData, FullLocalPath = message.LocalPath, Success = true });
                            }
                            catch(Exception ex)
                            {
                                Logger.Log(LogPriority.High, "Server #{0} error writing file: {1}", ServerId, message.LocalPath);
                                ReceiveEnd(this, new ServerEventArgs() { FileData = FileMetaData, FullLocalPath = message.LocalPath, Success = false });
                            }
                        }
                        break;
                    }


                case MessageIdentifier.Verification:
                    {
                        VerificationMessage message = currentMessage as VerificationMessage;
                        if (message != null)
                        {
                            if (message.Key == connection.LocalAccessKey)
                            {
                                result = new VerificationMessage(connection.RemoteAccessKey, NetworkResponse.Valid);

                                //store validation result for later use
                                ClientHasBeenValidated = true;
                            }
                        }
                        break;
                    }

            }


            return result;
        }
    }
}
