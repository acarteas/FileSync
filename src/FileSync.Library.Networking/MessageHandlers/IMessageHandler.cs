using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public interface IMessageHandler
    {
        bool Process(BinaryReader reader, BinaryWriter writer);
    }
}
