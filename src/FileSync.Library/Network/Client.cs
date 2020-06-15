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
        public FileMetaData DataToSend { get; set; }

        public event EventHandler<ClientEventArgs> SendComplete = delegate { };

        public Client(Connection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }


        public void SendFile()
        {
            ClientEventArgs args = new ClientEventArgs();
            args.FileName = DataToSend.Path;

            TcpClient client = new TcpClient(_connection.Address, _connection.Port);
            BufferedStream stream = new BufferedStream(client.GetStream());
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);
            try
            {
                //introduce ourselves
                IMessage toServer = new IntroMessage(_connection.RemoteAccessKey, FileSyncOperation.SendFile, DataToSend);
                byte[] introBytes = toServer.ToBytes();
                writer.Write(IPAddress.HostToNetworkOrder(introBytes.Length));
                writer.Write(introBytes);

                //get server response
                int serverResponseBytes = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                IntroMessage serverIntroResponse = new IntroMessage();
                if(serverResponseBytes > 0)
                {
                    serverIntroResponse = new IntroMessage(reader.ReadBytes(serverResponseBytes));
                }

                //verify that server accepted our key and that their response key matches our local key
                if (serverIntroResponse.Response == NetworkResponse.Valid && _connection.LocalAccessKey == serverIntroResponse.Key)
                {
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
            

            
        }
    }
}
