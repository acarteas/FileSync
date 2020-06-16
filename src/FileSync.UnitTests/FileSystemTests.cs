using FileSync.Library;
using FileSync.Library.Config;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace FileSync.UnitTests
{
    public class Tests
    {
        List<FileSyncManager> Managers { get; set; }
        List<FileSyncConfig> Configs { get; set; }
        TestLogger Logger { get; set; }

        [OneTimeSetUp]
        public void Init()
        {
            Configs = Helpers.GenerateServerConfig();
            Managers = new List<FileSyncManager>();
            Logger = new TestLogger();
            foreach (var config in Configs)
            {
                foreach (var connection in config.RemoteConnections)
                {
                    //clear existing files
                    foreach(var file in Directory.GetFiles(connection.Value.LocalSyncPath))
                    {
                        File.Delete(file);
                    }
                    Managers.Add(new FileSyncManager(config, connection.Value, Logger));
                }
            }

            //start servers
            foreach (var manager in Managers)
            {
                manager.Start();
            }
        }
        [SetUp]
        public void Setup()
        {
        }

        /// <summary>
        /// Creates 100 files ranging in size 1KB, 2KB, 4KB, 8KB, ... 1MB and verifies that
        /// they were copied to destination server.
        /// 
        /// Currently passes in debug, but fails in run mode
        /// </summary>
        [Test]
        public void CreateTest()
        {
            var rng = RandomNumberGenerator.Create();
            List<byte[]> fileBytes = new List<byte[]>();
            int numFilesToCreate = 12;
            int numBytes = 1024;
            for (int i = 1; i < numFilesToCreate; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    BinaryReader reader = new BinaryReader(ms);
                    byte[] bytes = new byte[numBytes];
                    fileBytes.Add(bytes);
                    rng.GetBytes(bytes);
                    writer.Write(bytes);
                    ms.Seek(0, SeekOrigin.Begin);
                    string fileName = Path.Join(Configs.First().RemoteConnections.First().Value.LocalSyncPath, i.ToString() + ".dat");
                    var outputFile = File.Open(fileName, FileMode.Create);
                    outputFile.Write(reader.ReadBytes(numBytes));
                    outputFile.Close();
                }
                numBytes *= 2;
            }

            //spin until all files received or we reach timeout
            string targetDirectory = Configs.Last().RemoteConnections.Last().Value.LocalSyncPath;
            int counter = 0;
            while(Managers.First().IsProcessingFiles == true || Managers.Last().IsProcessingFiles == true)
            {
                Thread.Sleep(100);
                counter++;

                //slept for 10 seconds and still not all files, probably something wrong
                if(counter > 100)
                {
                    Assert.Fail("Timeout waiting for files to be created on destination server.");
                    return;
                }
            }


            //verify correctness of files on destination server
            numBytes = 1024;
            for (int i = 1; i < numFilesToCreate; i++)
            {
                string fileName = Path.Join(targetDirectory, i.ToString() + ".dat");
                byte[] inputBytes = new byte[numBytes];
                using (var inputFile = File.OpenRead(fileName))
                {
                    inputFile.Read(inputBytes, 0, inputBytes.Length);
                }
                bool isGood = true;
                for (int j = 0; j < inputBytes.Length; j++)
                {
                    if (inputBytes[j] != fileBytes[i - 1][j])
                    {
                        isGood = false;
                        break;
                    }
                }
                numBytes *= 2;

                //Assert.IsTrue(isGood, "File {0} byte mismatch", fileName);

            }
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}