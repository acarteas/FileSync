using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class FileRequestMessage : FileChangedMessage
    {
        public override MessageIdentifier MessageId { get { return MessageIdentifier.FileRequest; } }

        public FileRequestMessage(FileMetaData md) : base(md)
        {
        }

        public FileRequestMessage(byte[] bytes) : base(bytes)
        {
        }

        public FileRequestMessage(BinaryReader reader) : base(reader)
        {
        }
    }
}
