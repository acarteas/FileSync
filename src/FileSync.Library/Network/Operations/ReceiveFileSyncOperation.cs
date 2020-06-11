using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public class ReceiveFileSyncOperation : NetworkOperation
    {
        public FileSyncOperation Operation { get; set; }
        public ReceiveFileSyncOperation(BinaryReader reader, BinaryWriter writer, ILogger logger) : base(reader, writer, logger)
        {
        }

        public override bool Run()
        {
            try
            {
                Operation = (FileSyncOperation)IPAddress.NetworkToHostOrder(Reader.ReadInt32());
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            return true;
        }
    }
}
