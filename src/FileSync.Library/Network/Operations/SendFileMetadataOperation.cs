using FileSync.Library.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public class SendFileMetadataOperation : NetworkOperation
    {
        public FileMetaData FileData { get; set; }
        public SendFileMetadataOperation(TcpClient client, FileMetaData data, ILogger logger) : base(client, logger)
        {
            FileData = data;
        }

        public override bool Run()
        {
            BinaryWriter writer = null;
            BinaryReader reader = null;
            string jsonFileOp = JsonConvert.SerializeObject(FileData);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonFileOp);
            bool success = false;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);
                writer.Write(IPAddress.HostToNetworkOrder(jsonBytes.Length));
                writer.Write(jsonBytes);
                success = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            finally
            {
                writer.Close();
                reader.Close();
            }
            return success;
        }
    }
}
