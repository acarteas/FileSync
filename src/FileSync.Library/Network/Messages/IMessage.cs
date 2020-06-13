using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public interface IMessage
    {
        void FromBytes(byte[] bytes);
        byte[] ToBytes();
    }
}
