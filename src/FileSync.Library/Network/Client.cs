using FileSync.Library.Config;
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

        protected bool Validate(TcpClient client)
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
                var bufferedStream = new BufferedStream(client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);

                //send our local key to server for verification
                byte[] key = Convert.FromBase64String(_config.LocalAccessKey);
                writer.Write(IPAddress.HostToNetworkOrder(key.Length));
                writer.Write(key);

                //receive response message
                AuthResponse authResponse = (AuthResponse)IPAddress.NetworkToHostOrder(reader.ReadInt32());
                if (authResponse == AuthResponse.VALID)
                {
                    //validate server key
                    int serverKeyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] serverKeyBytes = reader.ReadBytes(serverKeyLength);
                    string serverKey = Convert.ToBase64String(serverKeyBytes);

                    //ensure valid server key before continuing
                    if (_config.RemoteConnections[_address.ToString()].AccessKey == serverKey)
                    {
                        //inform server that they check out as well
                        writer.Write((int)AuthResponse.VALID);
                        couldValidate = true;
                    }
                    else
                    {
                        //inform server that we couldn't validate their API key
                        writer.Write((int)AuthResponse.INVAID);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error validating server: {0}", ex.Message);
            }
            finally
            {
                writer.Close();
                reader.Close();
            }
            return couldValidate;
        }

        /// <summary>
        /// Sends file info to server in order to make a determination if the server needs an updated version
        /// </summary>
        /// <returns>True when server requests updated copy, false otherwise</returns>
        protected bool SendFileInfo(TcpClient client, FileInfo toSend)
        {
            BinaryWriter writer = null;
            BinaryReader reader = null;
            bool serverWantsFile = false;
            try
            {
                var bufferedStream = new BufferedStream(client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: {0}", ex.Message);
            }
            finally
            {
                writer.Close();
                reader.Close();
            }
            return serverWantsFile;
        }

        public void SendFile()//FileInfo toSend)
        {
            TcpClient client = null;
            BinaryWriter writer = null;
            BinaryReader reader = null;
            ClientEventArgs args = new ClientEventArgs()
            {
                //FileName = toSend.FullName,
                WasSuccessful = false
            };
            try
            {
                client = new TcpClient(_address.ToString(), _port);
                var bufferedStream = new BufferedStream(client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);

                if(Validate(client) == true)
                {

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: {0}", ex.Message);
            }
            finally
            {
                writer.Close();
                reader.Close();
                client.Close();
            }

            //notify owner that we are all done
            SendComplete(this, args);
        }
    }
}
