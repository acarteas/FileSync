using FileSync.Library;
using FileSync.Library.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace FileSync.Testing
{
    public class NetworkTests
    {
        List<FileSyncManager> Managers { get; set; }
        List<FileSyncConfig> Configs { get; set; }
        TestLogger Logger { get; set; }

        public void Init()
        {
            Configs = Helpers.GenerateServerConfig();
            Managers = new List<FileSyncManager>();
            Logger = new TestLogger();
            foreach (var config in Configs)
            {
                foreach (var share in config.Shares)
                {
                    //clear existing files
                    foreach (var file in Directory.GetFiles(share.Key))
                    {
                        File.Delete(file);
                    }
                    Managers.Add(new FileSyncManager(config, share.Value, Logger));
                }
            }

            //start servers
            foreach (var manager in Managers)
            {
                manager.Start();
            }
        }

        public void Teardown()
        {
            foreach (var manager in Managers)
            {
                manager.Stop();
            }
        }

        public void Wait()
        {
            //wait for file processing to finish before exiting
            while (Managers.First().IsProcessingFiles || Managers.Last().IsProcessingFiles)
            {
                Thread.Sleep(100);
            }
        }

        //Creates empty files and verifies that they were copied to the server.
        public void Create()
        {
            int numFilesToCreate = 12;

            //listen for changes on destination
            Managers.Last().FileReceived += (object sender, Library.Network.ServerEventArgs e) =>
            {
                string sourceFilePath = Path.Join(Managers.First().Share.Path, e.FileData.Path);
                string destinationFilePath = e.FullLocalPath;
                FileInfo sfi = new FileInfo(sourceFilePath);
                FileInfo dfi = new FileInfo(destinationFilePath);
                Assert.AreEqual(sfi.Length, dfi.Length, "Source and destination file sizes differ on {0}", e.FileData.Path);
            };

            for (int i = 0; i < numFilesToCreate; i++)
            {
                string fileName = Path.Join(Configs.First().Shares.First().Key, i.ToString() + ".dat");
                var outputFile = File.Open(fileName, FileMode.Create);
                outputFile.Close();
            }
        }


        /// <summary>
        /// Updates files ranging in size 1KB, 2KB, 4KB, 8KB, ... 1MB and verifies that
        /// they were copied to destination server.
        /// </summary>
        public void UpdateTest()
        {
            var rng = RandomNumberGenerator.Create();
            int numFilesToCreate = 12;
            int numBytes = 1024;

            //listen for changes on destination
            Managers.Last().FileReceived += (object sender, Library.Network.ServerEventArgs e) =>
            {
                string sourceFilePath = Path.Join(Managers.First().Share.Path, e.FileData.Path);
                string destinationFilePath = e.FullLocalPath;
                FileInfo sfi = new FileInfo(sourceFilePath);
                FileInfo dfi = new FileInfo(destinationFilePath);
                Assert.AreEqual(sfi.Length, dfi.Length, "Source and destination file sizes differ on {0}", e.FileData.Path);

                //verify correctness of files on destination server
                using (var sourceFile = sfi.OpenRead())
                {
                    using (var destFile = dfi.OpenRead())
                    {
                        byte[] sourceBuffer = new byte[1024];
                        byte[] destBuffer = new byte[1024];
                        while (sourceFile.Read(sourceBuffer) > 0 && destFile.Read(destBuffer) > 0)
                        {
                            Assert.AreEqual(sourceBuffer, destBuffer);
                            bool isGood = true;
                            for (int i = 0; i < sourceBuffer.Length; i++)
                            {
                                if (sourceBuffer[i] != destBuffer[i])
                                {
                                    isGood = false;
                                    break;
                                }
                            }
                            Assert.IsTrue(isGood, "File {0} byte mismatch", e.FileData.Path);
                        }
                    }
                }
            };
            for (int i = 1; i < numFilesToCreate; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    BinaryReader reader = new BinaryReader(ms);
                    byte[] bytes = new byte[numBytes];
                    rng.GetBytes(bytes);
                    writer.Write(bytes);
                    ms.Seek(0, SeekOrigin.Begin);
                    string fileName = Path.Join(Configs.First().Shares.First().Key, i.ToString() + ".dat");
                    var outputFile = File.Open(fileName, FileMode.Create, FileAccess.Write);
                    outputFile.Write(reader.ReadBytes(numBytes));
                    outputFile.Close();
                }
                numBytes *= 2;
            }
        }

        /// <summary>
        /// Renames files ensuring that the rename took place on the client
        /// </summary>
        public void RenameTest()
        {
            //listen for changes on destination
            Managers.Last().FileReceived += (object sender, Library.Network.ServerEventArgs e) =>
            {
                string sourceFilePath = Path.Join(Managers.First().Share.Path, e.FileData.Path);
                string destinationFilePath = e.FullLocalPath;
                FileInfo sfi = new FileInfo(sourceFilePath);
                FileInfo dfi = new FileInfo(destinationFilePath);
                Assert.AreEqual(sfi.Length, dfi.Length, "Source and destination file sizes differ on {0}", e.FileData.Path);
            };

            //change file names on source server
            foreach(var filePath in Directory.GetFiles(Managers.First().Share.Path))
            {
                FileInfo fi = new FileInfo(filePath);
                var newName = Path.Join(fi.DirectoryName, Path.GetFileNameWithoutExtension(fi.Name) + "_a.dat");
                bool keepGoing = true;
                while(keepGoing)
                {
                    try
                    {
                        File.Move(fi.FullName, newName);
                        keepGoing = false;
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(100);
                    }
                }
                
            }
        }

        /// <summary>
        /// Deletes files ensuring that the delete took place on the client
        /// </summary>
        public void DeleteTest()
        {
            //listen for changes on destination
            Managers.Last().FileReceived += (object sender, Library.Network.ServerEventArgs e) =>
            {
                string sourceFilePath = Path.Join(Managers.First().Share.Path, e.FileData.Path);
                string destinationFilePath = e.FullLocalPath;
                FileInfo sfi = new FileInfo(sourceFilePath);
                FileInfo dfi = new FileInfo(destinationFilePath);
                Assert.AreEqual(sfi.Exists, dfi.Exists, "File still exists on client: {0}", e.FileData.Path);
            };

            //delete file names on source server
            foreach (var filePath in Directory.GetFiles(Managers.First().Share.Path))
            {
                bool keepGoing = true;
                while (keepGoing)
                {
                    try
                    {
                        File.Delete(filePath);
                        keepGoing = false;
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(100);
                    }
                }

            }
        }
    }
}
