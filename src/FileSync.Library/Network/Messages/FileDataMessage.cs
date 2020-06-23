using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class FileDataMessage : IMessage
    {
        public MessageIdentifier MessageId { get { return MessageIdentifier.FileData; } }
        public string FilePath { get; set; }
        public int BufferSize { get; set; }
        public FileDataMessage(string filePath = "")
        {
            FilePath = filePath;
            BufferSize = 1024;
        }
        public void FromBinaryStream(BinaryReader reader)
        {
            using (var fileWriter = new BinaryWriter(new BufferedStream(File.Open(FilePath, FileMode.Create))))
            {
                long remainingBytes = IPAddress.NetworkToHostOrder(reader.ReadInt64());
                do
                {
                    //next read will be the smaller of the max buffer size or remaining bytes
                    int bytesToRequest = (BufferSize > remainingBytes) ? (int)remainingBytes : BufferSize;
                    byte[] buffer = reader.ReadBytes(bytesToRequest);
                    fileWriter.Write(buffer);
                    remainingBytes -= bytesToRequest;
                } while (remainingBytes > 0);
            }
        }

        public void FromBytes(byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            FromBinaryStream(new BinaryReader(ms));
        }

        public byte[] ToBytes()
        {
            byte[] result = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((int)MessageId));
                    result = ms.ToArray();
                }
            }
            return result;
        }
    }
}
