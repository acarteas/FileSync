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
    /// Used by a server to verify a client
    /// </summary>
    public class ReceiveValidationOperation : NetworkOperation
    {
        public Connection Connection { get; set; }
        public ReceiveValidationOperation(BinaryReader reader, BinaryWriter writer, ILogger logger, Connection connection) : base(reader, writer, logger)
        {
            Connection = connection;
        }


        public override bool Run()
        {
            /*
             * Format for key exchange:
            Receive: INT-32 (length of client's API key)
            Receive: BYTE[] (client's API key)
            Send: INT-32 (AuthResponse of API key verification)
            Send: INT-32 (our key length for client verification)
            Send: BYTE[] (our API key)
            Receive: INT-32 (Client's AuthResponse of API key verification)
             * */
            bool couldValidate = false;
            try
            {
                int clientKeyLength = IPAddress.NetworkToHostOrder(Reader.ReadInt32());
                byte[] clientKeyBytes = Reader.ReadBytes(clientKeyLength);
                string clientKey = Convert.ToBase64String(clientKeyBytes);
                if (clientKey == Connection.LocalAccessKey)
                {
                    //tell client that we accept their key
                    Writer.Write(IPAddress.HostToNetworkOrder((int)AuthResponse.Valid));

                    //send our copy of client key for verification
                    byte[] localKeyBytes = Convert.FromBase64String(Connection.RemoteAccessKey);
                    Writer.Write(IPAddress.HostToNetworkOrder(localKeyBytes.Length));
                    Writer.Write(localKeyBytes);

                    //check to make sure client is okay with our key
                    AuthResponse response = (AuthResponse)(IPAddress.NetworkToHostOrder(Reader.ReadInt32()));
                    couldValidate = response == AuthResponse.Valid;
                }
                else
                {
                    Writer.Write(IPAddress.HostToNetworkOrder((int)AuthResponse.Invalid));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error validating client: {0}", ex.Message);
            }
            return couldValidate;
        }
    }
}
