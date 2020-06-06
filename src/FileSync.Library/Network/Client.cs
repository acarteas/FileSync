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
    public class Client
    {
        IPAddress _address = null;
        int _port = 13000;
        FileSystemConfig _config;

        public Client(FileSystemConfig config, string address, int port)
        {
            _config = config;
            _port = port;
            _address = IPAddress.Parse(address);
        }

        public void SendFile()//FileInfo toSend)
        {
            TcpClient client = null;
            BinaryWriter writer = null;
            BinaryReader reader = null;
            try
            {
                client = new TcpClient(_address.ToString(), _port);
                var bufferedStream = new BufferedStream(client.GetStream());
                writer = new BinaryWriter(bufferedStream);
                reader = new BinaryReader(bufferedStream);

                //send our local key to server for verification
                byte[] key = Convert.FromBase64String(_config.LocalAccessKey);
                writer.Write(IPAddress.HostToNetworkOrder(key.Length));
                writer.Write(key);

                //receive response message
                AuthResponse authResponse = (AuthResponse)IPAddress.NetworkToHostOrder(reader.ReadInt32());
                if(authResponse == AuthResponse.VALID)
                {
                    //validate server key
                    int serverKeyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] serverKeyBytes = reader.ReadBytes(serverKeyLength);
                    string serverKey = Convert.ToBase64String(serverKeyBytes);

                    //ensure valid server key before continuing
                    if(_config.RemoteConnections[_address.ToString()].AccessKey == serverKey)
                    {
                        //both parties have been validated, send file...
                    }
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
        }
    }
}
