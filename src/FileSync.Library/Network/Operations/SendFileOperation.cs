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
        public SendFileOperation(BinaryReader reader, BinaryWriter writer, ILogger logger, string filePath) : base(reader, writer, logger)
        {
            FilePath = filePath;
        }

        public override bool Run()
        {
            BinaryReader fileReader = null;
            try
            {
                fileReader = new BinaryReader(File.OpenRead(FilePath));
                byte[] buffer;
                while((buffer = fileReader.ReadBytes(BUFFER_SIZE)).Length > 0)
                {
                    Writer.Write(buffer);
                }
            }
            catch (Exception ex)
            {

                Logger.Log("Exception: {0}", ex.Message);
            }
            finally
            {
                fileReader.Close();
            }
            return true;
        }
    }
}
