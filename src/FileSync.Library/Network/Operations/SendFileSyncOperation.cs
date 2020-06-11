using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public class SendFileSyncOperation : NetworkOperation
    {
        public FileSyncOperation Operation { get; set; }
        public SendFileSyncOperation(BinaryReader reader, BinaryWriter writer, ILogger logger, FileSyncOperation operation) : base(reader, writer, logger)
        {
            Operation = operation;
        }

        public override bool Run()
        {
            try
            {
                Writer.Write(IPAddress.HostToNetworkOrder((int)Operation));
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            return true;
        }
    }
}
