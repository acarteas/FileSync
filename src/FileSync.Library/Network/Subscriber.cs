using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileSync.Library.Network
{

    //https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=netcore-3.1  
    //https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example?redirectedfrom=MSDN
    public class Subscriber
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public void Listen()
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                Console.Write("Waiting for a connection... ");
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    server.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), server);
                    allDone.WaitOne();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            TcpListener server = ar.AsyncState as TcpListener;
            if (server != null)
            {
                // Get the socket that handles the client request.
                Socket listener = server.Server;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                NetworkState state = new NetworkState();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, NetworkState.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            NetworkState state = (NetworkState)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();

                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);

                /*
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.  
                    //Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, NetworkState.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
                */
            }
        }
    }
}
