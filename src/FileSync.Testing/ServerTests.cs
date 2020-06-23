using FileSync.Library.Config;
using FileSync.Library.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Testing
{
    public class ServerTests
    {
        private List<FileSyncConfig> Configs { get; set; }
        private TestLogger Logger { get; set; }
        private Server Server { get; set; }
        private TcpListener Listener { get; set; }
        public void Init()
        {
            Configs = Helpers.GenerateServerConfig();
            Logger = new TestLogger();
            foreach (var config in Configs)
            {
                foreach (var connection in config.RemoteConnections)
                {
                    //clear existing files
                    foreach (var file in Directory.GetFiles(connection.Value.LocalSyncPath))
                    {
                        File.Delete(file);
                    }
                }
            }
            Listener = new TcpListener(IPAddress.Any, Configs.First().LocalListenPort);
            Listener.Start();
            Server = new Server(Configs.First(), Listener, Logger);
        }

        public void VerificationTest()
        {

        }
    }
}
