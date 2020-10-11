using FileSync.Library.Networking;
using FileSync.Library.Networking.MessageHandlers;
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
            var result = client.Get("test.txt", localFilePath);
            string savedText = File.ReadAllText(localFilePath);
            FileInfo localFileInfo = new FileInfo(localFilePath);

            Assert.AreEqual(result, true);
            Assert.AreEqual(testFileText, savedText);
            Assert.AreEqual(remoteFileInfo.CreationTimeUtc.Ticks, localFileInfo.CreationTimeUtc.Ticks);
            Assert.AreEqual(remoteFileInfo.LastWriteTimeUtc.Ticks, localFileInfo.LastWriteTimeUtc.Ticks);

            Console.WriteLine();
        }

        private void PutTest()
        {
            Console.WriteLine("Put Test");

            var currentDirectory = Directory.GetCurrentDirectory();
            string localDirectory = Path.Combine(currentDirectory, "put");
            if(Directory.Exists(localDirectory) == false)
            {
                Directory.CreateDirectory(localDirectory);
            }
            string localFilePath = Path.Combine(localDirectory, "test.txt");
            string localText = "PutTest 123";
            File.WriteAllText(localFilePath, localText);
            FileInfo localFileInfo = new FileInfo(localFilePath);

            string dest = Path.Combine("put", "test.txt");
            var result = client.Put(dest, localFilePath);
            var share = server.Config.Shares.FirstOrDefault();
            string testFilePath = Path.Combine(share.Value.AbsolutePath, dest);
            FileInfo remoteFileInfo = new FileInfo(testFilePath);
            string remoteFileText = File.ReadAllText(testFilePath);

            Assert.AreEqual(result, true);
            Assert.AreEqual(localText, remoteFileText);
            Assert.AreEqual(remoteFileInfo.CreationTimeUtc.Ticks, localFileInfo.CreationTimeUtc.Ticks);
            Assert.AreEqual(remoteFileInfo.LastWriteTimeUtc.Ticks, localFileInfo.LastWriteTimeUtc.Ticks);

            Console.WriteLine();
        }

        private void InvalidKeyTest()
        {
            Console.WriteLine("Invalid Key Test");
            var share = server.Config.Shares.FirstOrDefault();
            string testFilePath = Path.Combine(share.Value.AbsolutePath, "test.txt");

            var goodKey = client.Config.AuthKey;
            client.Config.AuthKey = goodKey + "abcdef";
            var result = client.Get("test.txt", testFilePath);
            client.Config.AuthKey = goodKey;
            Assert.AreEqual(false, result);
            Console.WriteLine();
        }

        private void StopTest()
        {
            Console.WriteLine("Stop Test");
            server.Stop();
            Thread.Sleep(2005);
            Assert.AreEqual(false, server.IsRunning, "Server still running after stop issued.");
            Assert.AreEqual(ServerThread.IsAlive, false, "Server still running after stop issued.");
            Console.WriteLine();
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
            PutTest();
            InvalidKeyTest();
            StopTest();
        }
    }
}
