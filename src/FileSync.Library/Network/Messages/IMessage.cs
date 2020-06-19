using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Library.Network.Messages
{

    public interface IMessage
    {
        MessageIdentifier MessageId { get; }
        void FromBinaryStream(BinaryReader reader);
        void FromBytes(byte[] bytes);
        byte[] ToBytes();
    }
}
