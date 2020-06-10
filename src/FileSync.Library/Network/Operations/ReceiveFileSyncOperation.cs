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
        public ReceiveFileSyncOperation(TcpClient client, ILogger logger) : base(client, logger)
        {
        }

        public override bool Run()
        {
            BinaryReader reader = null;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                reader = new BinaryReader(bufferedStream);
                Operation = (FileSyncOperation)IPAddress.NetworkToHostOrder(reader.ReadInt32());
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            finally
            {
                reader.Close();
            }
            return true;
        }
    }
}
