using FileSync.Library.Networking.MessageHandlers;
using FileSync.Library.Shared.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Networking
{
    public class FileSyncServer
    {

        private static int _server_counter = 1;

        private bool ShouldRun { get; set; }
        public ServerConfig Config { get; private set; }
        public ILogger Logger { get; set; }
        public TcpListener Listener { get; protected set; }
        public int ServerId { get; private set; }
        public bool IsRunning { get; private set; }

        public FileSyncServer(ServerConfig config, TcpListener listener, ILogger logger)
        {
            ShouldRun = true;
            Listener = listener;
            ServerId = _server_counter;
            _server_counter++;
            Logger = logger;
            Config = config;
        }

        /// <summary>
        /// Will stop the server from accepting new clients.
        /// </summary>
        public void Stop()
        {
            ShouldRun = false;
        }

        public void Start()
        {
            IsRunning = true;
            Logger.Log("Server #{0} listening for connections...", ServerId);
            MessageHandlerFactory factory = new MessageHandlerFactory() { Config = Config };
            while (ShouldRun == true)
            {
                TcpClient client = null;
                try
                {
                    //nested WHILE on same ShouldRun should allow us to gracefully terminate the server
                    while (ShouldRun == true)
                    {
                        var asyncRequest = Listener.AcceptTcpClientAsync();
                        asyncRequest.Wait(1000);
                        if (asyncRequest.IsCompletedSuccessfully)
                        {
                            client = asyncRequest.Result;
#if DEBUG == false
                        //timeouts affect debugging when stepping through code
                        client.ReceiveTimeout = 5000;
#endif
                            break;
                        }
                    }
                    if(ShouldRun == false)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Error accepting TCP client: {0}", ex.Message);
                    continue;
                }

                string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                Logger.Log("Server #{0} accepting client: {1}", ServerId, address);

                BufferedStream stream = new BufferedStream(client.GetStream());
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);
                try
                {

                    //read key
                    string key = Helpers.ReadString(reader);

                    //validate
                    if (Config.IsValidKey(key, address) == true)
                    {
                        //read op, process request 
                        factory.AuthKey = key;
                        MessageType requestedOp = (MessageType)reader.ReadByte();
                        IMessageHandler handler = factory.CreateMessageHander(requestedOp);
                        Logger.Log("Server #{0} requesting op {1}", ServerId, requestedOp);
                        handler.Process(reader, writer);
                        writer.Write((byte)MessageResponse.OK);
                    }
                    else
                    {
                        throw new ValidationException("Authentication key provided is invalid");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Server #{0} exception: {1}", ServerId, ex.Message);
                    writer.Write((byte)MessageResponse.Error);
                }
                finally
                {
                    Logger.Log("Server #{0} done handling client", ServerId);
                    reader.Close();
                    writer.Close();
                }
            } //END WHILE
            IsRunning = false;
        }
    }
}
