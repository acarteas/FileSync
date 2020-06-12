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
        public SendFileMetadataOperation(BinaryReader reader, BinaryWriter writer, FileMetaData data, ILogger logger) : base(reader, writer, logger)
        {
            FileData = data;
        }

        public override bool Run()
        {
            string jsonFileOp = JsonConvert.SerializeObject(FileData);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonFileOp);
            bool success = false;
            try
            {
                Writer.Write(IPAddress.HostToNetworkOrder(jsonBytes.Length));
                Writer.Write(jsonBytes);

                //positive result indicates server would like file
                int result = IPAddress.NetworkToHostOrder(Reader.ReadInt32());
                success = result > 0;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            return success;
        }
    }
}
