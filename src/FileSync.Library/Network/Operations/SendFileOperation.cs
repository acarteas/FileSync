using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public class SendFileOperation : NetworkOperation
    {
        public string FilePath { get; set; }
        public static readonly int BUFFER_SIZE = 1024;
        public SendFileOperation(TcpClient client, ILogger logger, string filePath) : base(client, logger)
        {
            FilePath = filePath;
        }

        public override bool Run()
        {
            BinaryWriter writer = null;
            BinaryReader reader = null;
            BinaryReader fileReader = null;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);
                fileReader = new BinaryReader(File.OpenRead(FilePath));
                byte[] buffer;
                while((buffer = fileReader.ReadBytes(BUFFER_SIZE)).Length > 0)
                {
                    writer.Write(buffer);
                }
            }
            catch (Exception ex)
            {

                Logger.Log("Exception: {0}", ex.Message);
            }
            finally
            {
                fileReader.Close();
                writer.Close();
                reader.Close();
            }
            return true;
        }
    }
}
