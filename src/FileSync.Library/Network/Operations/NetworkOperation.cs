using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public abstract class NetworkOperation : INetworkOperation
    {
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }
        public ILogger Logger { get; set; }
        public NetworkOperation(BinaryReader reader, BinaryWriter writer, ILogger logger)
        {
            Logger = logger;
            Reader = reader;
            Writer = writer;
        }
        public abstract bool Run();
    }
}
