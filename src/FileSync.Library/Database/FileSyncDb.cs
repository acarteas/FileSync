using FileSync.Library.Config;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace FileSync.Library.Database
{
    public class FileSyncDb
    {
        private static Dictionary<string, FileSyncDb> _instances = new Dictionary<string, FileSyncDb>();

        public FilesDb Files { get; private set; }

        private FileSyncDb(string share)
        {
            Files = new FilesDb(FileSystemDbConnection(share));
        }

        private void InitDb(string sharePath)
        {
            File.ReadAllText(Path.Join(Environment.CurrentDirectory, "Database", "fs.sql"));
        }

        public static FileSyncDb GetInstance(FileSyncShare share)
        {
            return GetInstance(share.Path);
        }

        public static FileSyncDb GetInstance(string sharePath)
        {
            if (_instances.ContainsKey(sharePath) == false)
            {
                _instances.Add(sharePath, new FileSyncDb(sharePath));
            }
            return _instances[sharePath];
        }

        public static string DbFile(string syncPath)
        {
            return Path.Join(Environment.CurrentDirectory, string.Join("fs_{0}.db", syncPath.GetHashCode()));
        }
        public static string DbFile(FileSyncShare share)
        {
            return DbFile(share.Path);
        }

        public static SQLiteConnection FileSystemDbConnection(string syncPath)
        {
            return new SQLiteConnection("Data Source=" + DbFile(syncPath));
        }

        public static SQLiteConnection FileSystemDbConnection(FileSyncShare share)
        {
            return FileSystemDbConnection(share.Path);
        }
    }
}
