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
                writer.Write(IPAddress.HostToNetworkOrder((int)MessageType.Get));
                Helpers.WriteString(Config.ShareName, writer);
                Helpers.WriteString(serverFilePath, writer);
                FileMetaData md = FileMetaData.FromReader(reader);
                Helpers.WriteFile(localFilePath, md, reader);
                Helpers.UpdateFileMetaData(localFilePath, md);
                                
            }
            catch (Exception ex)
            {
                Logger.Log("Error sending {0} to {1}: {2}", localFilePath, Config.ServerIpAddress, ex.Message);
            }
            return true;
        }

        public bool Put(string serverFilePath, string localFilePath)
        {
            try
            {
                TcpClient client = new TcpClient(Config.ServerIpAddress, Config.ServerPort);
                BufferedStream stream = new BufferedStream(client.GetStream());
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                Helpers.WriteString(Config.AuthKey, writer);
                writer.Write(IPAddress.HostToNetworkOrder((int)MessageType.Put));
                Helpers.WriteString(Config.ShareName, writer);
                Helpers.WriteString(serverFilePath, writer);
                FileMetaData md = Helpers.GetMetaData(localFilePath);
                Helpers.ReadFile(localFilePath, md, writer);

                //TODO: handle bad response 
            }
            catch (Exception ex)
            {
                Logger.Log("Error sending {0} to {1}: {2}", localFilePath, Config.ServerIpAddress, ex.Message);
            }
            return true;
        }
    }
}
