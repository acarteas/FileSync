using FileSync.Library.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileSync.Testing
{
    public class FileSystemTests
    {
        private void CreateFiles(int numFilesToCreate)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            int startBytes = 1024;
            int numBytes = startBytes;
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
                    string fileName = Path.Join(i.ToString() + ".dat");
                    var outputFile = File.Open(fileName, FileMode.Create, FileAccess.Write);
                    outputFile.Write(reader.ReadBytes(numBytes));
                    outputFile.Close();
                }
                numBytes = i * startBytes;
            }
        }

        public async void ScanForFilesTest()
        {
            CreateFiles(100);
            Watcher watcher = new Watcher("Z:/Music");
            await watcher.ScanForUpdates();
        }
    }
}
