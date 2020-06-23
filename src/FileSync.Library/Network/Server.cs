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

//expected stream format: 
//INT-32 (length of auth key)
//BYTE[] (auth key)
//AUTH-KEY (512 bytes)
//INT-32 (length of file name)
//STRING-UTF8 (file name)
//INT-32 (length relative path location)
//STRING-UTF8 (relative path location)
//BYTE[] (binary file data)
namespace FileSync.Library.Network
{
    //TODO: add option for SSL communication (tutorial at https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1)
    public class Server
    {
        private bool Run { get; set; }
        private FileSyncConfig Config { get; set; }
        private int ServerId { get; set; }
        private bool ClientHasBeenValidated { get; set; }
        private static int _server_counter = 1;
        private static readonly int BUFFER_SIZE = 1024;

        public event EventHandler<ServerEventArgs> ReceiveBegin = delegate { };
        public event EventHandler<ServerEventArgs> ReceiveEnd = delegate { };
        public ILogger Logger { get; set; }
        public TcpListener Listener { get; protected set; }
        public Server(FileSyncConfig config, TcpListener listener, ILogger logger)
        {
            Run = true;
            Config = config;
            Listener = listener;
            ServerId = _server_counter;
            _server_counter++;
            Logger = logger;
        }

        private void HandleFileUpdate(Connection activeConnection, FileMetaData metaData, BinaryReader networkReader)
        {
            //find location of file on our file system
            string filePath = Path.Join(activeConnection.LocalSyncPath, metaData.Path);
            ReceiveBegin(this, new ServerEventArgs() { FileData = metaData, FullLocalPath = filePath, Success = false });
            try
            {
                //handle delete and rename operations separately
                if (metaData.OperationType != WatcherChangeTypes.Renamed)
                {
                    FileInfo localFile = new FileInfo(filePath);

                    //if our copy is older than theirs, take it
                    if (localFile.Exists == false || localFile.LastWriteTimeUtc < metaData.LastWriteTimeUtc)
                    {
                        Logger.Log("Receiving file \"{0}\" from client.", metaData.Path);

                        using (var fileWriter = new BinaryWriter(new BufferedStream(File.Open(filePath, FileMode.Create))))
                        {
                            long remainingBytes = IPAddress.NetworkToHostOrder(networkReader.ReadInt64());
                            do
                            {
                                //next read will be the smaller of the max buffer size or remaining bytes
                                int bytesToRequest = (BUFFER_SIZE > remainingBytes) ? (int)remainingBytes : BUFFER_SIZE;
                                byte[] buffer = networkReader.ReadBytes(bytesToRequest);
                                fileWriter.Write(buffer);
                                remainingBytes -= bytesToRequest;
                            } while (remainingBytes > 0);
                        }

                        //change last write to match client file
                        File.SetLastWriteTimeUtc(filePath, metaData.LastWriteTimeUtc);
                        File.SetLastAccessTimeUtc(filePath, metaData.LastAccessTimeUtc);
                        File.SetCreationTimeUtc(filePath, metaData.CreateTimeUtc);

                    }
                }
                else if (metaData.OperationType == WatcherChangeTypes.Renamed)
                {
                    //no need to send file over network if all we're doing is a rename or delete
                    string oldFilePath = Path.Join(activeConnection.LocalSyncPath, metaData.OldPath);
                    if (File.Exists(oldFilePath))
                    {
                        File.Move(oldFilePath, filePath);
                    }
                }
                else if (metaData.OperationType == WatcherChangeTypes.Deleted)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                ReceiveEnd(this, new ServerEventArgs() { FileData = metaData, FullLocalPath = filePath, Success = true });
            }
            catch (Exception ex)
            {
                Logger.Log("Thread #{0} encountered issues with {1}: {2}", ServerId, metaData.Path, ex.Message);
                ReceiveEnd(this, new ServerEventArgs() {FileData = metaData, FullLocalPath = filePath, Success = false });
            }
            finally
            {

            }
        }

        public void Stop()
        {
            Run = false;
        }

        public void Start()
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

                //new client has not been validated
                ClientHasBeenValidated = false;

                Logger.Log("Server #{0} accepting client: {1}", ServerId, client.Client.RemoteEndPoint);

                //reject connections not stored in our config
                string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                if (Config.RemoteConnections.ContainsKey(address) == true)
                {
                    Connection activeConnection = Config.RemoteConnections[address];
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
                            HandleFileUpdate(connection, message.FileData, reader);
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
