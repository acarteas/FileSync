﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class NullMessage : IMessage
    {
        public MessageIdentifier MessageId { get { return MessageIdentifier.Null } }

        public NullMessage()
        {

        }

        public NullMessage(byte[] bytes)
        {
            FromBytes(bytes);
        }

        public NullMessage(BinaryReader reader)
        {
            FromBinaryStream(reader);
        }

        public void FromBinaryStream(BinaryReader reader)
        {
            //The message ID is in the stream as an identifier for the message factory but is
            //unnecessary for altering our present state
            int messageId = IPAddress.NetworkToHostOrder(reader.ReadInt32());
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
                    result = ms.ToArray();
                }
            }
            return result;
        }
    }
}
