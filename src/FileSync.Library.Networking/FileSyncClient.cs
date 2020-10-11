using FileSync.Library.Networking.MessageHandlers;
using FileSync.Library.Shared.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Networking
{
    public class FileSyncClient
    {
        public ILogger Logger { get; set; }
        public ClientConfig Config { get; set; }
        
        public FileSyncClient(ClientConfig config, ILogger logger)
        {
            Config = config;
            Logger = logger;
        }


        public bool Get(string serverFilePath, string localFilePath)
        {

            try
            {
                TcpClient client = new TcpClient(Config.ServerIpAddress, Config.ServerPort);
                BufferedStream stream = new BufferedStream(client.GetStream());
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                Helpers.WriteString(Config.AuthKey, writer);
                writer.Write((byte)MessageType.Get);
                Helpers.WriteString(Config.ShareName, writer);
                Helpers.WriteString(serverFilePath, writer);
                FileMetaData md = FileMetaData.FromReader(reader);
                Helpers.WriteFile(localFilePath, md, reader);
                Helpers.UpdateFileMetaData(localFilePath, md);

                MessageResponse response = (MessageResponse)reader.ReadByte();
                return response == MessageResponse.OK;
                                
            }
            catch (Exception ex)
            {
                Logger.Log("Error sending {0} to {1}: {2}", localFilePath, Config.ServerIpAddress, ex.Message);
            }
            return false;
        }

        public bool Put(string serverFilePath, string localFilePath)
        {
            try
            {
                using (TcpClient client = new TcpClient(Config.ServerIpAddress, Config.ServerPort))
                {
                    using(BufferedStream stream = new BufferedStream(client.GetStream()))
                    {
                        using(BinaryReader reader = new BinaryReader(stream))
                        {
                            using (BinaryWriter writer = new BinaryWriter(stream))
                            {
                                Helpers.WriteString(Config.AuthKey, writer);
                                writer.Write((byte)MessageType.Put);
                                Helpers.WriteString(Config.ShareName, writer);
                                Helpers.WriteString(serverFilePath, writer);
                                FileMetaData md = Helpers.GetMetaData(localFilePath);
                                writer.Write(md.ToBytes());
                                Helpers.ReadFile(localFilePath, md, writer);

                                var result = (MessageResponse)reader.ReadByte();
                                return result == MessageResponse.OK;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error sending {0} to {1}: {2}", localFilePath, Config.ServerIpAddress, ex.Message);
            }
            return true;
        }
    }
}
