using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
                fileWriter = new BinaryWriter(new BufferedStream(File.OpenWrite(DestinationFilePath)));
                byte[] buffer;
                while ((buffer = Reader.ReadBytes(BUFFER_SIZE)).Length > 0)
                {
                    fileWriter.Write(buffer);
                }
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
