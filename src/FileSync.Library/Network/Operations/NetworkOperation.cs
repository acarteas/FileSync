using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public abstract class NetworkOperation : INetworkOperation
    {
        public TcpClient Client { get; set; }
        public ILogger Logger { get; set; }
        public NetworkOperation(TcpClient client, ILogger logger)
        {
            Client = client;
            Logger = logger;
        }
        public abstract bool Run();
    }
}
