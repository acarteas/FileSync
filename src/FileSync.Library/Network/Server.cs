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
        public static readonly int BUFFER_SIZE = 1024;
        private static int _thread_counter = 1;
        private int _thread_id;
        public ILogger Logger { get; set; }
        protected FileSyncConfig _config;
        private static Dictionary<string, int> _activeFiles = new Dictionary<string, int>();

        public event EventHandler<ServerEventArgs> ReceiveBegin = delegate { };
        public event EventHandler<ServerEventArgs> ReceiveEnd = delegate { };

        public TcpListener Listener { get; protected set; }
        public Server(FileSyncConfig config, TcpListener listener, ILogger logger)
        {
            Run = true;
            _config = config;
            Listener = listener;
            _thread_id = _thread_counter;
            _thread_counter++;
            Logger = logger;
        }

        private void HandleFileUpdate(Connection activeConnection, FileMetaData metaData, BinaryReader networkReader)
        {
            //find location of file on our file system
            string filePath = Path.Join(activeConnection.LocalSyncPath, metaData.Path);
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
            }
            catch (Exception ex)
            {
                Logger.Log("Thread #{0} encountered issues with {1}: {2}", _thread_id, metaData.Path, ex.Message);
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
                Logger.Log("Server #{0} waiting for connection...", _thread_id);
                TcpClient client = null;
                try
                {
                    client = Listener.AcceptTcpClient();

#if DEBUG == false
                    //timeouts affect debugging when stepping through code
                    client.ReceiveTimeout = 5000;
#endif

                }
                catch(Exception ex)
                {
                    Logger.Log("Could not accept TCP client: {0}", ex.Message);
                    Run = false;
                    continue;
                }
                
                Logger.Log("Server #{0} accepting client: {1}", _thread_id, client.Client.RemoteEndPoint);

                //reject connections not stored in our config
                string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                if (_config.RemoteConnections.ContainsKey(address) == true)
                {
                    Connection activeConnection = _config.RemoteConnections[address];
                    BufferedStream stream = new BufferedStream(client.GetStream());
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    try
                    {
                        IMessage result = ProcessData(activeConnection, reader);
                        while(result.MessageId != MessageIdentifier.Null)
                        {
                            writer.Write(result.ToBytes());
                            result = ProcessData(activeConnection, reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Server #{0} exception: {1}", _thread_id, ex.Message);
                    }
                    finally
                    {
                        Logger.Log("Server #{0} done handling client", _thread_id);
                        reader.Close();
                        writer.Close();
                    }
                }
            }
        }

        protected IMessage ProcessData(Connection connection, BinaryReader reader)
        {
            IMessage result = MessageFactory.FromStream(reader);
            switch (result.MessageId)
            {
                case MessageIdentifier.FileChanged:
                    break;

                case MessageIdentifier.Verification:
                    break;
            }


            return result;
        }
    }
}
