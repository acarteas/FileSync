using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public class PutMessageHandler : IMessageHandler
    {
        public string AuthKey { get; set; }
        public ServerConfig Config { get; set; }
        public PutMessageHandler()
        {

        }

        public bool Process(BinaryReader reader, BinaryWriter writer)
        {
            string shareName = Helpers.ReadString(reader);
            if (Config.HasShareAccess(AuthKey, shareName) == true)
            {
                string relativeFilePath = Helpers.ReadString(reader);
                string localFilePath = Config.GetFilePath(shareName, relativeFilePath);

                string directoryPath = Path.GetDirectoryName(localFilePath);
                if(Directory.Exists(directoryPath) == false)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                FileMetaData md = FileMetaData.FromReader(reader);
                Helpers.WriteFile(localFilePath, md, reader);
                Helpers.UpdateFileMetaData(localFilePath, md);
            }
            return true;
        }
    }
}
