using FileSync.Library.Config;
using FileSync.Library.Logging;
using FileSync.Library.Network.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace FileSync.Library.Network
{
    //TODO: add option for SSL communication (tutorial at https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1)
    public class Client
    {
        public static readonly int BUFFER_SIZE = 1024;
        private Connection _connection;
        private ILogger _logger;

        public event EventHandler<ClientSendEventArgs> SendComplete = delegate { };

        public Client(Connection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }

        private bool Handshake(BinaryReader reader, BinaryWriter writer)
        {
            IMessage toServer = new VerificationMessage(_connection.RemoteAccessKey);
            byte[] introBytes = toServer.ToBytes();
            writer.Write(IPAddress.HostToNetworkOrder(introBytes.Length));
            writer.Write(introBytes);

            //get server response
            int serverResponseBytes = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            VerificationMessage serverIntroResponse = new VerificationMessage();
            if (serverResponseBytes > 0)
            {
                serverIntroResponse = new VerificationMessage(reader.ReadBytes(serverResponseBytes));
            }
            if(serverIntroResponse.Response == NetworkResponse.Valid && serverIntroResponse.Key == _connection.LocalAccessKey)
            {
                return true;
            }
            return false;
        }

        public ClientSendEventArgs SendFile(FileMetaData data)
        {
            ClientSendEventArgs args = new ClientSendEventArgs();
            args.FileData = data;

            TcpClient client = new TcpClient(_connection.Address, _connection.Port);
#if DEBUG == false
            //timeouts don't work well when you're debugging
            client.SendTimeout = 5000;
#endif

            BufferedStream stream = new BufferedStream(client.GetStream());
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);
            try
            {
                //introduce ourselves
                if (Handshake(reader, writer) == true)
                {
                    //tell server that we would like to send them a file
                    

                    //On certain operations (e.g. rename, delete), there is no need to send the whole file to the server.
                    //In such an event, the server will respond with a Null FileSyncOperation.  
                    if (client.Connected == true && serverIntroResponse.RequestedOperation == FileSyncOperation.SendFile)
                    {
                        string basePath = _connection.LocalSyncPath;
                        string localFilePath = Path.Join(basePath, DataToSend.Path);
                        if (File.Exists(localFilePath))
                        {
                            FileInfo toSend = new FileInfo(localFilePath);
                            using (var fileReader = new BinaryReader(File.OpenRead(localFilePath)))
                            {
                                byte[] buffer;
                                writer.Write(IPAddress.HostToNetworkOrder(toSend.Length));
                                while ((buffer = fileReader.ReadBytes(BUFFER_SIZE)).Length > 0)
                                {
                                    writer.Write(buffer);
                                }
                                writer.Flush();
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Log("Error: {0}", ex.Message);
            }
            finally
            {
                client.Close();

                //notify owner that we are all done
                SendComplete(this, args);
            }

            return args;
        }
    }
}
