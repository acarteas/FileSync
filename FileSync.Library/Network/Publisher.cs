using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSync.Library.Network
{
    //tcp documentation: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=netcore-3.1
    public class Publisher
    {
        public async void Send(String message)
        {
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                TcpClient client = new TcpClient(localAddr.ToString(), port);

                // Convert the string data to byte data using ASCII encoding.  
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Begin sending the data to the remote device.  
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

                // Close everything.
                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }

    }
}
