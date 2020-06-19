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
            //The message ID is in the stream as an identifier for the message factory but is
            //unnecessary for altering our present state
            int keyLength = 0;
            int messageId = IPAddress.NetworkToHostOrder(reader.ReadInt32());
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
