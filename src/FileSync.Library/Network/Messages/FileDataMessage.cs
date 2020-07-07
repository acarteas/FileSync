using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class FileDataMessage : IMessage
    {
        public MessageIdentifier MessageId { get { return MessageIdentifier.FileData; } }
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
        public int BufferSize { get; set; }
        public long FileSize { get; set; }
        public BinaryReader FileStream { get; set; }
        public FileDataMessage(string filePath = "")
        {
            LocalPath = filePath;
            BufferSize = 1024;
        }

        public FileDataMessage(BinaryReader reader) : this("")
        {
            FromBinaryStream(reader);
        }

        public void FromBinaryStream(BinaryReader reader)
        {
            //assume incoming stream will also contain actual file data
            FileStream = reader;

            int remotePathLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            if(remotePathLength > 0)
            {
                byte[] remotePathBytes = reader.ReadBytes(remotePathLength);
                RemotePath = Encoding.UTF8.GetString(remotePathBytes);
            }
            int localPathLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            if(localPathLength > 0)
            {
                byte[] localPathBytes = reader.ReadBytes(localPathLength);
                LocalPath = Encoding.UTF8.GetString(localPathBytes);
            }
            FileSize = IPAddress.NetworkToHostOrder(reader.ReadInt64());

            //actual file data receive must be explicitly called
        }

        private void WriteFileDataHelper(BinaryReader from, BinaryWriter to)
        {
            long remainingBytes = FileSize;
            do
            {
                //next read will be the smaller of the max buffer size or remaining bytes
                int bytesToRequest = (BufferSize > remainingBytes) ? (int)remainingBytes : BufferSize;
                byte[] buffer = from.ReadBytes(bytesToRequest);
                to.Write(buffer);
                remainingBytes -= bytesToRequest;
            } while (remainingBytes > 0);
        }

        /// <summary>
        /// Sends the remaining data in the file stream to a local file
        /// </summary>
        public void WriteFileData()
        {
            using (var fileWriter = new BinaryWriter(new BufferedStream(File.Open(LocalPath, FileMode.Create))))
            {
                WriteFileDataHelper(FileStream, fileWriter);
            }
        }

        public void FromBytes(byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            FromBinaryStream(new BinaryReader(ms));
        }

        public void ToStream(BinaryWriter writer)
        {
            writer.Write(IPAddress.HostToNetworkOrder((int)MessageId));
            writer.Write(IPAddress.HostToNetworkOrder(RemotePath.Length));
            if (RemotePath.Length > 0)
            {
                writer.Write(Encoding.UTF8.GetBytes(RemotePath));
            }
            writer.Write(IPAddress.HostToNetworkOrder(LocalPath.Length));
            if (LocalPath.Length > 0)
            {
                writer.Write(Encoding.UTF8.GetBytes(LocalPath));
            }
            writer.Write(IPAddress.NetworkToHostOrder(FileSize));
            if(FileSize > 0)
            {
                WriteFileDataHelper(FileStream, writer);
            }
        }

        /// <summary>
        /// Not implemented as it seems like a bad idea to allow a user to store an entire file inside memory
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            throw new Exception("class FileDataMessage does not implement ToBytes");
        }
    }
}
