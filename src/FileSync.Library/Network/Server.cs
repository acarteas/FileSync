using FileSync.Library.Config;
using FileSync.Library.Logging;
using FileSync.Library.Network.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

//expected stream format: 
//INT-32 (length of auth key)
//BYTE[] (auth key)
//AUTH-KEY (512 bytes)
//INT-32 (length of file name)
//STRING-UTF8 (file name)
//INT-32 (length relative path location)
//STRING-UTF8 (relative path location)
//BYTE[] (binary file data)
namespace FileSync.Library.Network
{
    //TODO: add option for SSL communication (tutorial at https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1)
    public class Server
    {
        public static readonly int BUFFER_SIZE = 1024;
        public ILogger Logger { get; set; }
        protected FileSystemConfig _config;

        public TcpListener Listener { get; protected set; }
        public Server(FileSystemConfig config, TcpListener listener)
        {
            _config = config;
            Listener = listener;
        }

        public void Start()
        {
            BinaryReader reader = null;
            BinaryWriter writer = null;
            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;

            Debug.WriteLine("Waiting for connection...");
            var client = Listener.AcceptTcpClient();
            Debug.WriteLine("Accepting client: {0}", client.Client.RemoteEndPoint);

            //verify client
            var validator = new ReceiveValidationOperation(client, Logger, _config);
            bool isValidated = validator.Run();
            if(isValidated == true)
            {
                //determine client intent
                var opReader = new ReceiveFileSyncOperation(client, Logger);
                opReader.Run();
                FileSyncOperation op = opReader.Operation;

                //build appropraite response based on intent

            }
            client.Close();
        }
    }
}
