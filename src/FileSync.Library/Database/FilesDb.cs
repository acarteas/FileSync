using Dapper;
using FileSync.Library.Config;
using FileSync.Library.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync.Library.Database
{
    public class FilesDb
    {
        private DbConnection _db;
        public FilesDb(DbConnection db)
        {
            _db = db;
            _db.Open();
        }

        ~FilesDb()
        {
            _db.Close();
        }

        public async Task<bool> Add(FsFile file)
        {
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
                new { path = file.Path, size = file.Size, last_modified = file.Ticks });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return affectedRows == 1;
        }

        public async Task<bool> AddOrUpdate(FsFile file)
        {
            if (await Exists(file))
            {
                return await Update(file);
            }
            return await Add(file);
        }

        public async Task<bool> Exists(FsFile file)
        {
            string sql = @"SELECT id FROM files WHERE path=@path";
            var result = await _db.QueryAsync<int>(sql, new { path = file.Path });
            if(result.Count() > 0)
            {
                file.Id = result.First();
                return true;
            }
            return false;
        }

        public async Task<FsFile> Get(int id)
        {
            string sql = @"SELECT 
                           id AS Id,
                            path AS Path,
                            size AS Size,
                            last_modified AS Ticks
                           FROM files
                           WHERE id = @id";
            var result = await _db.QueryAsync<FsFile>(sql, new { id = id });
            return result.FirstOrDefault();
        }

        public async Task<List<FsFile>> GetMoreRecentThan(DateTime dt)
        {
            string sql = @"SELECT 
                           id AS Id,
                            path AS Path,
                            size AS Size,
                            last_modified AS Ticks
                           FROM files
                           WHERE last_modified > @lastModified
                           ORDER BY last_modified ASC
                            ";
            var result = await _db.QueryAsync<FsFile>(sql, new { lastModified = dt.ToUniversalTime().Ticks });
            return result.ToList();

        }

        public async Task<bool> Update(FsFile file)
        {
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
                new { id = file.Id, path = file.Path, size = file.Size, last_modified = file.Ticks });
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
            }
            return affectedRows == 1;
        }
    }
}
