using FileSync.Library.Config;
using FileSync.Library.Logging;
using FileSync.Library.Network.Operations;
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

            TcpClient client = new TcpClient(_connection.Address, _connection.Port);
            BufferedStream stream = new BufferedStream(client.GetStream());
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);
            try
            {
                //send over auth token
                var auth = new SendValidationOperation(reader, writer, _logger, _connection);
                if (auth.Run() == true)
                {
                    //send over file metadata
                    var metadata = new SendFileMetadataOperation(reader, writer, DataToSend, _logger);
                    metadata.Run();

                    //server will close connection if it decides that it doesn't want the file
                    if (client.Connected == true)
                    {
                        string basePath = _connection.LocalSyncPath;
                        string localFilePath = Path.Combine(basePath, DataToSend.Path);
                        if (File.Exists(localFilePath))
                        {
                            var fileOperation = new SendFileOperation(reader, writer, _logger, localFilePath);
                            fileOperation.Run();
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
            }
            

            //notify owner that we are all done
            SendComplete(this, args);
        }
    }
}
