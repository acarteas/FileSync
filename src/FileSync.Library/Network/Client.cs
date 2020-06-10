using FileSync.Library.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace FileSync.Library.Network
{
    //TODO: add option for SSL communication (tutorial at https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1)
    public class Client
    {
        private IPAddress _address = null;
        private int _port = 13000;
        private FileSystemConfig _config;

        public event EventHandler<ClientEventArgs> SendComplete = delegate { };

        public Client(FileSystemConfig config, string address, int port)
        {
            _config = config;
            _port = port;
            _address = IPAddress.Parse(address);
        }

        

        public void SendFile()
        {
            ClientEventArgs args = new ClientEventArgs();
            

            //notify owner that we are all done
            SendComplete(this, args);
        }
    }
}
