using FileSync.Library.Config;
using FileSync.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    /// <summary>
    /// Used by a client to verify a server
    /// </summary>
    public class SendValidationOperation : NetworkOperation
    {
        Connection Connection { get; set; }
        public SendValidationOperation(BinaryReader reader, BinaryWriter writer, ILogger logger, Connection connection) : base(reader, writer, logger)
        {
            Connection = connection;
        }
        

        public override bool Run()
        {
            /*
             * Format for key exchange:
            Send: INT-32 (length of our local API key)
            Send: BYTE[] (local API key)
            Receive: INT-32 (AuthResponse API key verification)
            Receive: INT-32 (lenght of server's API key for verification)
            Receive: BYTE[] (server API key)
            Send: INT-32 (AuthResponse API key verification)
             * */
            bool couldValidate = false;
            try
            {

                //send our server key to server for verification
                byte[] key = Convert.FromBase64String(Connection.RemoteAccessKey);
                Writer.Write(IPAddress.HostToNetworkOrder(key.Length));
                Writer.Write(key);

                //receive response message
                AuthResponse authResponse = (AuthResponse)IPAddress.NetworkToHostOrder(Reader.ReadInt32());
                if (authResponse == AuthResponse.Valid)
                {
                    //validate server key
                    int serverKeyLength = IPAddress.NetworkToHostOrder(Reader.ReadInt32());
                    byte[] serverKeyBytes = Reader.ReadBytes(serverKeyLength);
                    string serverKey = Convert.ToBase64String(serverKeyBytes);

                    //ensure valid server key before continuing
                    if (Connection.LocalAccessKey == serverKey)
                    {
                        //inform server that they check out as well
                        Writer.Write(IPAddress.HostToNetworkOrder((int)AuthResponse.Valid));
                        couldValidate = true;
                    }
                    else
                    {
                        //inform server that we couldn't validate their API key
                        Writer.Write(IPAddress.HostToNetworkOrder((int)AuthResponse.Invalid));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error validating server: {0}", ex.Message);
            }
            return couldValidate;
        }
    }
}
