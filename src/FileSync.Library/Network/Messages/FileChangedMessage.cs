using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class FileChangedMessage : IMessage
    {
        public virtual MessageIdentifier MessageId { get { return MessageIdentifier.FileChanged; } }
        public FileMetaData FileData { get; private set; }

        public FileChangedMessage(FileMetaData md)
        {
            FileData = md;
        }

        public FileChangedMessage(byte[] bytes)
        {
            FromBytes(bytes);
        }

        public FileChangedMessage(BinaryReader reader)
        {
            FromBinaryStream(reader);
        }

        public void FromBinaryStream(BinaryReader reader)
        {
            int fdByteLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            FileData = FileMetaData.FromBytes(reader.ReadBytes(fdByteLength));
        }

        public void FromBytes(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    FromBinaryStream(reader);
                }
            }
        }

        public byte[] ToBytes()
        {
            byte[] result = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((int)MessageId));
                    byte[] fdBytes = FileData.ToBytes();
                    writer.Write(IPAddress.HostToNetworkOrder(fdBytes.Length));
                    writer.Write(fdBytes);
                }
                result = ms.ToArray();
            }
            return result;
        }
    }
}
