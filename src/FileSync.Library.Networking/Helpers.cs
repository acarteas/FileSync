using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Networking
{
    public class Helpers
    {
        public static FileMetaData GetMetaData(string filePath)
        {
            FileInfo info = new FileInfo(filePath);
            FileMetaData metaData = new FileMetaData()
            {
                LastWriteTimeUtc = info.LastWriteTimeUtc,
                LastAccessTimeUtc = info.LastAccessTimeUtc,
                CreateTimeUtc = info.CreationTimeUtc,
                FileSize = info.Length
            };
            return metaData;
        }

        public static void UpdateFileMetaData(string filePath, FileMetaData md)
        {
            File.SetLastWriteTimeUtc(filePath, md.LastWriteTimeUtc);
            File.SetLastAccessTimeUtc(filePath, md.LastAccessTimeUtc);
            File.SetCreationTimeUtc(filePath, md.CreateTimeUtc);
        }

        public static void WriteFile(string filePath, FileMetaData md, BinaryReader source)
        {
            using (var fileWriter = new BinaryWriter(new BufferedStream(File.Open(filePath, FileMode.OpenOrCreate))))
            {
                long remainingBytes = md.FileSize;
                int bufferSize = 1024;
                while (remainingBytes > 0)
                {
                    int bytesToRequest = (bufferSize > remainingBytes) ? (int)remainingBytes : bufferSize;
                    byte[] buffer = source.ReadBytes(bytesToRequest);
                    fileWriter.Write(buffer);
                    remainingBytes -= bytesToRequest;
                }
            }
        }

        public static void ReadFile(string filePath, FileMetaData md, BinaryWriter sink)
        {
            using (var fileReader = new BinaryReader(new BufferedStream(File.Open(filePath, FileMode.Open))))
            {
                long remainingBytes = md.FileSize;
                int bufferSize = 1024;
                while (remainingBytes > 0)
                {
                    int bytesToRequest = (bufferSize > remainingBytes) ? (int)remainingBytes : bufferSize;
                    byte[] buffer = fileReader.ReadBytes(bytesToRequest);
                    sink.Write(buffer);
                    remainingBytes -= bytesToRequest;
                }
            }
        }

        public static void WriteString(string text, BinaryWriter writer)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            writer.Write(IPAddress.HostToNetworkOrder(bytes.Length));
            writer.Write(bytes);
        }

        public static string ReadString(BinaryReader reader)
        {
            int length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        public static string ReadString(BinaryReader reader, int maxStringLength)
        {
            int length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            if(length > maxStringLength)
            {
                throw new Exception(string.Format("string length ({0}) exceeded. Max: {1}", length, maxStringLength));
            }
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }
    }
}
