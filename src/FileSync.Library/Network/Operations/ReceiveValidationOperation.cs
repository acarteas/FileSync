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
        public FileSystemConfig Config { get; set; }
        public ReceiveValidationOperation(TcpClient client, ILogger logger, FileSystemConfig config) : base(client, logger)
        {
            Config = config;
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
            BinaryReader reader = null;
            BinaryWriter writer = null;
            bool couldValidate = false;
            try
            {
                BufferedStream bufferedStream = new BufferedStream(Client.GetStream());
                reader = new BinaryReader(bufferedStream);
                writer = new BinaryWriter(bufferedStream);
                int clientKeyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                byte[] clientKeyBytes = reader.ReadBytes(clientKeyLength);
                string clientKey = Convert.ToBase64String(clientKeyBytes);
                string clientIpAddress = ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString();
                if (clientKey == Config.RemoteConnections[clientIpAddress].AccessKey)
                {
                    //tell client that we accept their key
                    writer.Write(IPAddress.HostToNetworkOrder((int)AuthResponse.Valid));

                    //send our key for verification
                    writer.Write(Config.LocalAccessKey.Length);
                    writer.Write(Convert.FromBase64String(Config.LocalAccessKey));

                    //check to make sure client is okay with our key
                    AuthResponse response = (AuthResponse)(IPAddress.NetworkToHostOrder(reader.ReadInt32()));
                    couldValidate = response == AuthResponse.Valid;
                }
                else
                {
                    writer.Write((int)AuthResponse.Invalid);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error validating client: {0}", ex.Message);
            }
            finally
            {
                reader.Close();
                writer.Close();
            }
            return couldValidate;
        }
    }
}
