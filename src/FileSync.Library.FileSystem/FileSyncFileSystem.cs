using FileSync.Library.FileSystem.Database;
using FileSync.Library.FileSystem.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSync.Library.FileSystem
{
    public class FileSyncFileSystem
    {
        private System.IO.FileSystemWatcher _watcher = null;
        private FileSyncDb _db;
        public event EventHandler<FsFileSystemEventArgs> FileChangeDetected = delegate { };
        public string PathToWatch { get; protected set; }

        public FileSyncFileSystem(string pathToWatch = ".")
        {
            PathToWatch = pathToWatch;
            _db = FileSyncDb.GetInstance(pathToWatch);
        }

        /// <summary>
        /// Returns a list of files who have changed after the supplied date
        /// </summary>
        /// <param name="minDate">The age at which to consider a file as "recent."  Files with a greater date will be returned.</param>
        /// <returns></returns>
        public async Task<List<FileInfo>> GetRecentFiles(DateTime minDate)
        {
            List<FsFile> dbFiles = await _db.Files.GetMoreRecentThan(minDate.ToUniversalTime());
            List<FileInfo> result = new List<FileInfo>();
            foreach (var dbFile in dbFiles)
            {
                result.Add(new FileInfo(Path.Join(PathToWatch, dbFile.Path)));
            }
            return result;
        }

        /// <summary>
        /// Scans the watched directory for files 
        /// </summary>
        /// <param name="minDate">Will only match files whose modified date is greater than the supplied parameter</param>
        /// <returns></returns>
        public async Task<int> ScanForFiles(DateTime minDate)
        {
            DateTime utcTime = minDate.ToUniversalTime();
            var files = Directory.EnumerateFiles(PathToWatch, "*.*", SearchOption.AllDirectories).Where(f => (new FileInfo(f)).LastWriteTimeUtc > utcTime).AsParallel();
            return await ScanForFilesHelper(files);
        }

        /// <summary>
        /// Scans the watched directory for files
        /// </summary>
        /// <returns></returns>
        public async Task<int> ScanForFiles()
        {
            var files = Directory.EnumerateFiles(PathToWatch, "*.*", SearchOption.AllDirectories).AsParallel();
            return await ScanForFilesHelper(files);
        }

        private async Task<int> ScanForFilesHelper(ParallelQuery<string> files)
        {
 
            //AC: I think I was getting an issue where the file search would terminate before all files had
            //been processed.  Forcing ToList() should make the work happen up front.
            var allFiles = files.ToList();
            foreach (var filePath in allFiles)
            {
                var fileInfo = new FileInfo(filePath);
                FsFile file = new FsFile() { LastModified = fileInfo.LastWriteTimeUtc, Path = FullToRelative(PathToWatch, fileInfo.FullName), Size = fileInfo.Length };
                await _db.Files.AddOrUpdate(file);
            }
            return allFiles.Count;
        }

        //based on code from https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netcore-3.1
        private void Start()
        {

            // Create a new FileSystemWatcher and set its properties.
            _watcher = new System.IO.FileSystemWatcher();
            _watcher.Path = PathToWatch;
            _watcher.IncludeSubdirectories = true;

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            _watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;

            // Example if we wanted to restrict file types
            //watcher.Filter = "*.txt";

            // Add event handlers.
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;

            // Begin watching.
            _watcher.EnableRaisingEvents = true;
        }

        private static string FullToRelative(string basePath, string fullPath)
        {
            return fullPath.Substring(basePath.Length + 1);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FsFileSystemEventArgs args = new FsFileSystemEventArgs
            {
                BaseArgs = e,
                RelativePath = FullToRelative(PathToWatch, e.FullPath)
            };

            //renamed and deleted events need to have the record removed from the DB
            if (e.ChangeType == WatcherChangeTypes.Renamed || e.ChangeType == WatcherChangeTypes.Deleted)
            {
                Task.Run(async () =>
                {
                    string oldFilePath = null;
                    switch (e.ChangeType)
                    {
                        case WatcherChangeTypes.Renamed:
                            oldFilePath = FullToRelative(PathToWatch, (e as RenamedEventArgs).OldFullPath);
                            break;

                        case WatcherChangeTypes.Deleted:
                            oldFilePath = FullToRelative(PathToWatch, e.FullPath);
                            break;
                    }
                    await _db.Files.Remove(oldFilePath);
                });
            }

            //everything except deletes will get a new or updated record
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                var fileInfo = new FileInfo(e.FullPath);
                FsFile file = new FsFile() { LastModified = fileInfo.LastWriteTimeUtc, Path = FullToRelative(PathToWatch, e.FullPath), Size = fileInfo.Length };
                Task.Run(async () =>
                {
                    await _db.Files.AddOrUpdate(file);
                    FileChangeDetected(this, args);
                });
            }
            else
            {
                //regardless, we need to fire a change event
                FileChangeDetected(this, args);
            }



        }

    }
}
