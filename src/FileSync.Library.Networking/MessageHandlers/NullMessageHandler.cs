using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public class NullMessageHandler : IMessageHandler
    {
        public bool Process(BinaryReader reader, BinaryWriter writer)
        {
            writer.Write(IPAddress.HostToNetworkOrder((int)MessageType.Null));
            return false;
        }
    }
}
