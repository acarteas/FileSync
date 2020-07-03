using FileSync.Library.Database;
using FileSync.Library.Database.Models;
using FileSync.Library.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSync.Testing
{
    public class FileSystemTests
    {
        private void CreateFiles(string basePath, int numFilesToCreate)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            int startBytes = 1024;
            int numBytes = startBytes;

            if(Directory.Exists(basePath) == false)
            {
                Directory.CreateDirectory(basePath);
            }

            for (int i = 1; i <= numFilesToCreate; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    BinaryReader reader = new BinaryReader(ms);
                    byte[] bytes = new byte[numBytes];
                    rng.GetBytes(bytes);
                    writer.Write(bytes);
                    ms.Seek(0, SeekOrigin.Begin);
                    string fileName = Path.Join(basePath, i.ToString() + ".dat");
                    var outputFile = File.Open(fileName, FileMode.Create, FileAccess.Write);
                    outputFile.Write(reader.ReadBytes(numBytes));
                    outputFile.Close();
                }
                numBytes = i * startBytes;
            }
        }

        private async Task ScanForFilesTest()
        {
            string serverPath = Path.Join(Environment.CurrentDirectory, "server1");
            DateTime now = DateTime.Now;
            Thread.Sleep(100);
            CreateFiles(serverPath, 25);

            //force refresh the DB for this test
            FileSyncDb db = FileSyncDb.GetInstance(serverPath, true);
            Watcher watcher = new Watcher(serverPath);
            await watcher.ScanForUpdates();

            //verify correctness
            List<FsFile> dbFiles = await db.Files.GetMoreRecentThan(now);
            Assert.AreEqual(dbFiles.Count, 25, "DbFiles count mismatch");

            foreach(FsFile dbFile in dbFiles)
            {
                string fullPath = Path.Join(serverPath, dbFile.Path);
                FileInfo fi = new FileInfo(fullPath);
                Assert.IsTrue(fi.Exists, "Db file does not exist on file system");
                Assert.AreEqual(fi.LastWriteTimeUtc, dbFile.LastModified, "Timestamp mismatch");
            }
        }

        /// <summary>
        /// Assumes that ScanForFilesTest has already been run.
        /// </summary>
        private async Task UpdatefilesTest()
        {
            string serverPath = Path.Join(Environment.CurrentDirectory, "server1");
            FileSyncDb db = FileSyncDb.GetInstance(serverPath);
            int changeCounter = 0;
            int filesToChange = 10;
            DateTime now = DateTime.Now;
            Thread.Sleep(100);

            Watcher watcher = new Watcher(serverPath);
            watcher.FileChangeDetected += async (object sender, FsFileSystemEventArgs args) =>
            {
                changeCounter++;

                FsFile changed = new FsFile() { Path = args.RelativePath };
                if(await db.Files.Exists(changed) == false)
                {
                    Assert.Fail("Expected item {0} does not exist in DB", changed.Path);
                }
                changed = await db.Files.Get(changed.Id);
                FileInfo info = new FileInfo(args.BaseArgs.FullPath);
                Assert.AreEqual(changed.LastModified, info.LastWriteTimeUtc, "Db record and file record mismatch on {0}", changed.Path);
            };
            watcher.Start();

            CreateFiles(serverPath, filesToChange);
            while (changeCounter < filesToChange)
            {
                Thread.Sleep(10);
            }
            watcher.Stop();
        }

        public async void Run()
        {
            Console.WriteLine("Scanning for files test...");
            await ScanForFilesTest();

            Console.Write("Watcher file update test...");
            await UpdatefilesTest();
        }
    }
}
