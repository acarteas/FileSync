using FileSync.Library.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network
{
    public class Server
    {
        public static readonly int BUFFER_SIZE = 1024;
        private FileSystemConfig _config;

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
            try
            {
                //expected stream format: 
                //INT-32 (length of auth key)
                //BYTE[] (auth key)
                //AUTH-KEY (512 bytes)
                //INT-32 (length of file name)
                //STRING-UTF8 (file name)
                //INT-32 (length relative path location)
                //STRING-UTF8 (relative path location)
                //BYTE[] (binary file data)

                string fileName = "Unknown";
                string fileLocation = "Unknown";
                BufferedStream bufferedStream = new BufferedStream(client.GetStream());
                reader = new BinaryReader(bufferedStream);
                writer = new BinaryWriter(bufferedStream);

                int clientKeyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                byte[] clientKeyBytes = reader.ReadBytes(clientKeyLength);
                string clientKey = Convert.ToBase64String(clientKeyBytes);
                string clientIpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                if(clientKey == _config.RemoteConnections[clientIpAddress].AccessKey)
                {
                    //tell client that we accept their key
                    writer.Write(IPAddress.HostToNetworkOrder((int)AuthResponse.VALID));

                    //send our key for verification
                    writer.Write(_config.LocalAccessKey.Length);
                    writer.Write(Convert.FromBase64String(_config.LocalAccessKey));
                }
                else
                {
                    writer.Write((int)AuthResponse.INVAID);
                }
                //int fileNameLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                //byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
                //fileName = Encoding.UTF8.GetString(fileNameBytes);
                //int fileLocationLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                //byte[] fileLocationBytes = reader.ReadBytes(fileLocationLength);
                //fileLocation = Encoding.UTF8.GetString(fileLocationBytes);

                /*
                string fullPath = Path.Join(fileName, fileLocation);
                using (BinaryWriter writer = new BinaryWriter(File.Open(fullPath, FileMode.Create)))
                {
                    while ((bytesRead = client.Client.Receive(buffer, BUFFER_SIZE, SocketFlags.None)) > 0)
                    {
                        writer.Write(buffer);
                    }
                }
                */
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Socket exception: {0}", ex.Message);
            }
            finally
            {
                reader.Close();
                writer.Close();
                client.Close();
            }

        }
    }
}
