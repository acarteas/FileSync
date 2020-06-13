using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class IntroMessage : IMessage
    {
        public string Key { get; set; }
        public FileSyncOperation RequestedOperation { get; set; }
        public FileMetaData MetaData { get; set; }
        public NetworkResponse Response { get; set; }

        public IntroMessage()
        {

        }

        public IntroMessage(string key, FileSyncOperation op, FileMetaData md, NetworkResponse response = NetworkResponse.Null)
        {
            Key = key;
            RequestedOperation = op;
            MetaData = md;
            Response = response;
        }

        public IntroMessage(byte[] bytes)
        {
            FromBytes(bytes);
        }

        public void FromBytes(byte[] bytes)
        {
            FileSyncOperation op = FileSyncOperation.Null;
            int keyLength = 0;
            int metaDataLength = 0;
            using(MemoryStream ms = new MemoryStream(bytes))
            {
                using(BinaryReader reader = new BinaryReader(ms))
                {
                    op = (FileSyncOperation)IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    Response = (NetworkResponse)IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    keyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    metaDataLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    Key = Encoding.UTF8.GetString(reader.ReadBytes(keyLength));
                    MetaData = FileMetaData.FromBytes(reader.ReadBytes(metaDataLength));
                }
            }
        }

        public byte[] ToBytes()
        {
            byte[] result = null;
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
            byte[] metaDataBytes = MetaData.ToBytes();
            using(MemoryStream ms = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((int)RequestedOperation));
                    writer.Write(IPAddress.HostToNetworkOrder((int)Response));
                    writer.Write(IPAddress.HostToNetworkOrder(keyBytes.Length));
                    writer.Write(IPAddress.HostToNetworkOrder(metaDataBytes.Length));
                    writer.Write(keyBytes);
                    writer.Write(metaDataBytes);
                    result = ms.ToArray();
                }
            }
            return result;
        }
    }
}
