using Dapper;
using FileSync.Library.Config;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync.Library.FileSystem
{
    public class FilesDb
    {
        private DbConnection _db;
        public FilesDb(DbConnection db)
        {
            _db = db;
        }

        public async Task<bool> Add(FsFile file)
        {
            _db.Open();
            string sql = @"INSERT INTO files (
                                  path,
                                  size,
                                  last_modified
                              )
                              VALUES (
                                  @path,
                                  @size,
                                  @last_modified
                              );";
            var affectedRows = 0;
            try
            {
                affectedRows = await _db.ExecuteAsync(sql,
                new { path = file.Path, size = file.Size, last_modified = file.LastModified.ToUniversalTime().ToString(Constants.TimeFormatString) });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            _db.Close();
            return affectedRows == 1;
        }

        public async Task<bool> AddOrUpdate(FsFile file)
        {
            if(await Exists(file))
            {
                return await Update(file);
            }
            return await Add(file);
        }

        public async Task<bool> Exists(FsFile file)
        {
            _db.Open();
            string sql = @"SELECT COUNT(id) FROM files WHERE path=@path";
            var count = await _db.ExecuteScalarAsync(sql, new { path = file.Path });
            _db.Close();
            int count_int = -1;
            Int32.TryParse(count.ToString(), out count_int);
            return count_int > 0;
        }

        public async Task<bool> Update(FsFile file)
        {
            _db.Open();
            string sql = @"UPDATE files
                           SET 
                               path = @path,
                               size = @size,
                               last_modified = @last_modified
                         WHERE id = @id ;";
            var affectedRows = 0;
            try
            {
                affectedRows = await _db.ExecuteAsync(sql,
                new { id = file.Id, path = file.Path, size = file.Size, last_modified = file.LastModified.ToUniversalTime().ToString(Constants.TimeFormatString) });
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
            }
            _db.Close();
            return affectedRows == 1;
        }
    }
}
