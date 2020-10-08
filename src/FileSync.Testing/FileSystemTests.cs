using FileSync.Library.FileSystem;
using FileSync.Library.Shared.Database;
using FileSync.Library.Shared.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            FileSyncFileSystem watcher = new FileSyncFileSystem(serverPath);
            await watcher.ScanForFiles();

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
        /// Assumes ScanForFilesTest has been run previously
        /// </summary>
        /// <returns></returns>
        private async Task ScanForFilesWithDateRestrictionTest()
        {
            string serverPath = Path.Join(Environment.CurrentDirectory, "server1");
            DateTime now = DateTime.Now;
            Thread.Sleep(100);
            CreateFiles(serverPath, 10);

            //force refresh the DB for this test
            FileSyncFileSystem watcher = new FileSyncFileSystem(serverPath);
            int result = await watcher.ScanForFiles(now);
            Assert.AreEqual(10, result, "FS Watcher did not find all 10 items");
        }

        /// <summary>
        /// Assumes ScanForFilesTest has been run previously
        /// </summary>
        private async Task GetRecentFilesTest()
        {
            string serverPath = Path.Join(Environment.CurrentDirectory, "server1");
            DateTime now = DateTime.Now;
            Thread.Sleep(100);
            int changeCounter = 0;
            int filesToChange = 10;

            FileSyncFileSystem watcher = new FileSyncFileSystem(serverPath);
            List<FileInfo> changedFiles = new List<FileInfo>();
            watcher.FileChangeDetected += (object sender, FsFileSystemEventArgs args) =>
            {
                changeCounter++;
                FileInfo fi = new FileInfo(args.BaseArgs.FullPath);
                changedFiles.Add(fi);
            };
            watcher.Start();
            CreateFiles(serverPath, filesToChange);
            while(changeCounter < filesToChange)
            {
                Thread.Sleep(100);
            }
            foreach(var fi in changedFiles)
            {
                var files = await watcher.GetRecentFiles(fi.LastWriteTimeUtc.AddTicks(-25));
                var query = files.Where(f => f.FullName == fi.FullName).Count();
                Assert.AreEqual(1, query, "DB did not contain updated item: {0}", fi.FullName);
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

            FileSyncFileSystem watcher = new FileSyncFileSystem(serverPath);
            List<FsFileSystemEventArgs> changedFiles = new List<FsFileSystemEventArgs>();
            watcher.FileChangeDetected += (object sender, FsFileSystemEventArgs args) =>
            {
                changeCounter++;
                changedFiles.Add(args);
            };
            watcher.Start();

            CreateFiles(serverPath, filesToChange);
            while (changeCounter < filesToChange)
            {
                Thread.Sleep(10);
            }
            foreach(var args in changedFiles)
            {
                if (await db.Files.Exists(args.RelativePath) < 1)
                {
                    Assert.Fail("Expected item {0} does not exist in DB", args.RelativePath);
                }
                int id = await db.Files.Exists(args.RelativePath);
                var changed = await db.Files.Get(id);
                FileInfo info = new FileInfo(args.BaseArgs.FullPath);
                Assert.AreEqual(changed.LastModified, info.LastWriteTimeUtc, "Db record and file record mismatch on {0}", changed.Path);
            }
        }

        public async void Run()
        {
            Console.WriteLine("ScanForFilesTest...");
            await ScanForFilesTest();

            Console.WriteLine("ScanForFilesWithDateRestrictionTest...");
            await ScanForFilesWithDateRestrictionTest();

            Console.WriteLine("GetRecentFilesTest...");
            GetRecentFilesTest();

            Console.Write("UpdatefilesTest..");
            UpdatefilesTest();

        }
    }
}
