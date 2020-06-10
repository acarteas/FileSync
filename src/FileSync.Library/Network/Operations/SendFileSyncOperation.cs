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
        public SendFileSyncOperation(TcpClient client, ILogger logger, FileSyncOperation operation) : base(client, logger)
        {
            Operation = operation;
        }

        public override bool Run()
        {
            BinaryWriter writer = null;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                writer.Write(IPAddress.HostToNetworkOrder((int)Operation));
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            finally
            {
                writer.Close();
            }
            return true;
        }
    }
}
