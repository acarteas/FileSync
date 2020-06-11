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
        public ReceiveFileMetadataOperation(BinaryReader reader, BinaryWriter writer, ILogger logger) : base(reader, writer, logger)
        {
        }

        public override bool Run()
        {
            FileMetaData fileOp = null;
            try
            {
                int length = IPAddress.NetworkToHostOrder(Reader.ReadInt32());
                byte[] rawBytes = Reader.ReadBytes(length);
                fileOp = JsonConvert.DeserializeObject<FileMetaData>(Encoding.UTF8.GetString(rawBytes));
            }
            catch (Exception ex)
            {

                Logger.Log("Exception: {0}", ex.Message);
            }
            FileData = fileOp;
            return fileOp != null;
        }
    }
}
