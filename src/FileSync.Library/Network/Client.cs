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

        public Client(Connection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }

        private bool Handshake(BinaryReader reader, BinaryWriter writer)
        {
            IMessage toServer = new VerificationMessage(_connection.RemoteAccessKey);
            byte[] introBytes = toServer.ToBytes();
            writer.Write(introBytes);

            //get server response
            VerificationMessage serverIntroResponse = MessageFactory.FromStream(reader) as VerificationMessage;
            if (serverIntroResponse.Response == NetworkResponse.Valid && serverIntroResponse.Key == _connection.LocalAccessKey)
            {
                return true;
            }
            return false;
        }

        public ClientSendResult SendFile(FileMetaData data)
        {
            ClientSendResult args = new ClientSendResult();
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
                    args.WasSuccessful = true;

                    //tell server that we would like to send them a file
                    IMessage message = new FileChangedMessage(data);
                    writer.Write(message.ToBytes());

                    //see if server wants the file
                    message = MessageFactory.FromStream(reader);

                    //server says they want the whole load
                    if(message.MessageId == MessageIdentifier.FileRequest)
                    {
                        message = new FileDataMessage();
                        writer.Write(message.ToBytes());

                        string basePath = _connection.LocalSyncPath;
                        string localFilePath = Path.Join(basePath, data.Path);
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
            catch(EndOfStreamException ex)
            { 
                //end of stream exception doesn't necessairly mean that the transfer was not successful so separate out
                //from generic exception
            }
            catch (Exception ex)
            {
                args.WasSuccessful = false;
                _logger.Log("Error: {0}", ex.Message);
            }
            finally
            {
                client.Close();
            }

            return args;
        }
    }
}
