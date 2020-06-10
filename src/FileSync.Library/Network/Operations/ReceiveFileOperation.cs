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
        public ReceiveFileOperation(TcpClient client, ILogger logger, string destination) : base(client, logger)
        {
            DestinationFilePath = destination;
        }

        public override bool Run()
        {
            BinaryWriter writer = null;
            BinaryReader reader = null;
            BinaryWriter fileWriter = null;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);
                fileWriter = new BinaryWriter(new BufferedStream(File.OpenWrite(DestinationFilePath)));
                byte[] buffer;
                while ((buffer = reader.ReadBytes(BUFFER_SIZE)).Length > 0)
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
                writer.Close();
                reader.Close();
            }
            return true;
        }
    }
}
