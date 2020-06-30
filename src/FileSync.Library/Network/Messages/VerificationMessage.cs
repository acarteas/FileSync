using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class VerificationMessage : IMessage
    {
        public string Key { get; set; }
        public NetworkResponse Response { get; set; }

        /// <summary>
        /// Major version of sender (i.e. X.y.y)
        /// </summary>
        public int VersionMajor { get; set; }

        /// <summary>
        /// Minor version of sender (i.e. y.X.y)
        /// </summary>
        public int VersionMinor { get; set; }

        /// <summary>
        /// Patch version of sender (i.e. y.y.X)
        /// </summary>
        public int VersionPatch { get; set; }

        public MessageIdentifier MessageId { get { return MessageIdentifier.Verification; } }

        public VerificationMessage()
        {

        }
        public VerificationMessage(string key, NetworkResponse response = NetworkResponse.Null)
        {
            Key = key;
            Response = response;
        }

        public VerificationMessage(byte[] bytes)
        {
            FromBytes(bytes);
        }

        public VerificationMessage(BinaryReader reader)
        {
            FromBinaryStream(reader);
        }

        public void FromBinaryStream(BinaryReader reader)
        {
            int keyLength = 0;
            VersionMajor = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            VersionMinor = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            VersionPatch = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            Response = (NetworkResponse)IPAddress.NetworkToHostOrder(reader.ReadInt32());
            keyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            if (keyLength > 0)
            {
                Key = Encoding.UTF8.GetString(reader.ReadBytes(keyLength));
            }
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
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((int)MessageId));
                    writer.Write(IPAddress.HostToNetworkOrder(VersionMajor));
                    writer.Write(IPAddress.HostToNetworkOrder(VersionMinor));
                    writer.Write(IPAddress.HostToNetworkOrder(VersionPatch));
                    writer.Write(IPAddress.HostToNetworkOrder((int)Response));
                    writer.Write(IPAddress.HostToNetworkOrder(keyBytes.Length));
                    writer.Write(keyBytes);
                    result = ms.ToArray();
                }
            }
            return result;
        }
    }
}
