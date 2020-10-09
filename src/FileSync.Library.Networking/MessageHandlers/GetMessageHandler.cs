using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public class GetMessageHandler : IMessageHandler
    {
        public string AuthKey { get; set; }
        public ServerConfig Config { get; set; }
        public GetMessageHandler()
        {

        }

        public bool Process(BinaryReader reader, BinaryWriter writer)
        {
            string shareName = Helpers.ReadString(reader);
            if (Config.HasShareAccess(AuthKey, shareName) == true)
            {
                string relativeFilePath = Helpers.ReadString(reader);
                string localFilePath = Config.GetFilePath(shareName, relativeFilePath);

                //begin writeback
                if (File.Exists(localFilePath))
                {
                    FileMetaData md = Helpers.GetMetaData(localFilePath);
                    writer.Write(md.ToBytes());
                    Helpers.ReadFile(localFilePath, md, writer);
                }
                else
                {
                    throw new FileNotFoundException(string.Format("Could not locate file: {0}", localFilePath));
                }
            }
            return true;
        }
    }
}
