using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public class ReceiveFileOperation : NetworkOperation
    {
        public static readonly int BUFFER_SIZE = 1024;
        public string DestinationFilePath { get; set; }
        public ReceiveFileOperation(BinaryReader reader, BinaryWriter writer, ILogger logger, string destination) : base(reader, writer, logger)
        {
            DestinationFilePath = destination;
        }

        public override bool Run()
        {
            BinaryWriter fileWriter = null;
            try
            {
                fileWriter = new BinaryWriter(new BufferedStream(File.Open(DestinationFilePath, FileMode.Create)));
                long remainingBytes = IPAddress.NetworkToHostOrder(Reader.ReadInt64());

                do
                {
                    //next read will be the smaller of the max buffer size or remaining bytes
                    int bytesToRequest = (BUFFER_SIZE > remainingBytes) ? (int)remainingBytes : BUFFER_SIZE;
                    byte[] buffer = Reader.ReadBytes(bytesToRequest);
                    fileWriter.Write(buffer);
                    remainingBytes -= bytesToRequest;
                } while (remainingBytes > 0);
                
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: {0}", ex.Message);
            }
            finally
            {
                fileWriter.Close();
            }
            return true;
        }
    }
}
