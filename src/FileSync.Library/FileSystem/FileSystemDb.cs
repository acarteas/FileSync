using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace FileSync.Library.FileSystem
{
    public class FileSystemDb
    {
        private static FileSystemDb _instance;

        public FilesDb Files { get; private set; }

        private FileSystemDb()
        {
            Files = new FilesDb(FileSystemDbConnection);
        }

        public static FileSystemDb GetInstance()
        {
            if (_instance == null)
            {
                _instance = new FileSystemDb();
            }
            return _instance;
        }

        public static string DbFile
        {
            get { return Environment.CurrentDirectory + "\\fs.db"; }
        }

        public static SQLiteConnection FileSystemDbConnection
        {
            get
            {
                return new SQLiteConnection("Data Source=" + DbFile);
            }
        }
    }
}
