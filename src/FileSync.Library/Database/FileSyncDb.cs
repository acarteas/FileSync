using Dapper;
using FileSync.Library.Config;
using FileSync.Library.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace FileSync.Library.Database
{
    public class FileSyncDb
    {
        private static Dictionary<string, FileSyncDb> _instances = new Dictionary<string, FileSyncDb>();

        public FilesDb Files { get; private set; }

        private FileSyncDb(string share, bool forceInit = false)
        {
            InitDb(share, forceInit).Wait();
            Files = new FilesDb(FileSystemDbConnection(share));
        }

        private static string Hash(string toHash)
        {
            string hash;
            using (SHA256 hasher = SHA256.Create())
            {
                var bytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(toHash));
                hash = Base32Encoding.ToString(bytes);
            }
            return hash;
        }

        private async Task<int> InitDb(string sharePath, bool forceInit = false)
        {
            string dbPath = Path.Join(Environment.CurrentDirectory, "fs.sql");
            string configPath = Path.Join(Environment.CurrentDirectory, string.Format("{0}_config.json", Hash(sharePath)));
            if(forceInit == true)
            {
                File.Delete(configPath);
                File.Delete(DbFile(sharePath));
            }
            if(File.Exists(configPath) == false)
            {
                DbConfig config = new DbConfig()
                {
                    BasePath = sharePath
                };
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config));

                var sql = File.ReadAllText(dbPath);
                DbConnection conn = FileSystemDbConnection(sharePath);
                var result = await conn.ExecuteAsync(sql);
                return result;
            }
            return -1;
        }

        public static FileSyncDb GetInstance(FileSyncShare share, bool forceInit = false)
        {
            return GetInstance(share.Path, forceInit);
        }

        public static FileSyncDb GetInstance(string sharePath, bool forceInit = false)
        {
            if (_instances.ContainsKey(sharePath) == false)
            {
                _instances.Add(sharePath, new FileSyncDb(sharePath, forceInit));
            }
            return _instances[sharePath];
        }

        public static string DbFile(string sharePath)
        {
            return Path.Join(Environment.CurrentDirectory, string.Format("{0}.db", Hash(sharePath)));
        }
        public static string DbFile(FileSyncShare share)
        {
            return DbFile(share.Path);
        }

        public static SQLiteConnection FileSystemDbConnection(string sharePath)
        {
            return new SQLiteConnection("Data Source=" + DbFile(sharePath));
        }

        public static SQLiteConnection FileSystemDbConnection(FileSyncShare share)
        {
            return FileSystemDbConnection(share.Path);
        }
    }
}
