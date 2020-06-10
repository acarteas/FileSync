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
    public class ReceiveFileMetadataOperation : NetworkOperation
    {
        public FileMetaData FileData { get; set; }
        public ReceiveFileMetadataOperation(TcpClient client, ILogger logger) : base(client, logger)
        {
        }

        public override bool Run()
        {
            BinaryWriter writer = null;
            BinaryReader reader = null;
            FileMetaData fileOp = null;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);
                int length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                byte[] rawBytes = reader.ReadBytes(length);
                fileOp = JsonConvert.DeserializeObject<FileMetaData>(Encoding.UTF8.GetString(rawBytes));
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
            FileData = fileOp;
            return fileOp != null;
        }
    }
}
