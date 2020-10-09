using FileSync.Library.Networking;
using FileSync.Library.Shared.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace FileSync.Testing
{
    public class NetworkTests
    {
        private FileSyncServer server;
        private FileSyncClient client;
        private Thread ServerThread;

        private void GetTest()
        {
            Console.WriteLine("Get Test");

            var share = server.Config.Shares.FirstOrDefault();
            string testFileText = "Hello, World!";
            string testFilePath = Path.Combine(share.Value.AbsolutePath, "test.txt");
            File.WriteAllText(testFilePath, testFileText);
            FileInfo remoteFileInfo = new FileInfo(testFilePath);

            Thread.Sleep(100);

            var currentDirectory = Directory.GetCurrentDirectory();
            string localFilePath = Path.Combine(currentDirectory, "test.txt");
            client.Get("test.txt", localFilePath);
            string savedText = File.ReadAllText(localFilePath);
            FileInfo localFileInfo = new FileInfo(localFilePath);

            Assert.AreEqual(testFileText, savedText);
            Assert.AreEqual(remoteFileInfo.CreationTimeUtc.Ticks, localFileInfo.CreationTimeUtc.Ticks);
            Assert.AreEqual(remoteFileInfo.LastWriteTimeUtc.Ticks, localFileInfo.LastWriteTimeUtc.Ticks);
        }

        private void PutTest()
        {
            Console.WriteLine("Put Test");
        }

        public void Run()
        {
            string json = File.ReadAllText("server_config.json");
            ServerConfig config = JsonConvert.DeserializeObject<ServerConfig>(json);
            TcpListener listener = new TcpListener(IPAddress.Any, config.ListenPort);
            listener.Start();

            server = new FileSyncServer(config, listener, new ConsoleLogger());
            ThreadStart ts = server.Start;
            ServerThread = new Thread(ts);
            ServerThread.Start();

            ClientConfig clientConfig = new ClientConfig()
            {
                AuthKey = "ABC123",
                ServerIpAddress = "127.0.0.1",
                ServerPort = config.ListenPort,
                ShareName = config.Shares.FirstOrDefault().Key
            };
            client = new FileSyncClient(clientConfig, new ConsoleLogger());

            GetTest();
        }
    }
}
