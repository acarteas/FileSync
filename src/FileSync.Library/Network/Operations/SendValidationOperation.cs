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
        public FileSystemConfig Config { get; set; }
        public IPAddress Address = null;
        public SendValidationOperation(TcpClient client, ILogger logger, IPAddress address, FileSystemConfig config) : base(client, logger)
        {
            Address = address;
            Config = config;
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
            BinaryWriter writer = null;
            BinaryReader reader = null;
            bool couldValidate = false;
            try
            {
                var bufferedStream = new BufferedStream(Client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);

                //send our local key to server for verification
                byte[] key = Convert.FromBase64String(Config.LocalAccessKey);
                writer.Write(IPAddress.HostToNetworkOrder(key.Length));
                writer.Write(key);

                //receive response message
                AuthResponse authResponse = (AuthResponse)IPAddress.NetworkToHostOrder(reader.ReadInt32());
                if (authResponse == AuthResponse.Valid)
                {
                    //validate server key
                    int serverKeyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] serverKeyBytes = reader.ReadBytes(serverKeyLength);
                    string serverKey = Convert.ToBase64String(serverKeyBytes);

                    //ensure valid server key before continuing
                    if (Config.RemoteConnections[Address.ToString()].AccessKey == serverKey)
                    {
                        //inform server that they check out as well
                        writer.Write((int)AuthResponse.Valid);
                        couldValidate = true;
                    }
                    else
                    {
                        //inform server that we couldn't validate their API key
                        writer.Write((int)AuthResponse.Invalid);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error validating server: {0}", ex.Message);
            }
            finally
            {
                writer.Close();
                reader.Close();
            }
            return couldValidate;
        }
    }
}
