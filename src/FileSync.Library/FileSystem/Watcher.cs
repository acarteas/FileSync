using FileSync.Library.Database;
using FileSync.Library.Database.Models;
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
    public class Watcher
    {
        private System.IO.FileSystemWatcher _watcher = null;
        private FileSyncDb _db;

        public event EventHandler<FsFileSystemEventArgs> FileChangeDetected = delegate { };
        public string PathToWatch { get; protected set; }
        protected bool ShouldRun { get; set; }
        public Thread RunningThread { get; protected set; }
        public Watcher(string pathToWatch = ".")
        {
            PathToWatch = pathToWatch;
            ShouldRun = true;
            ThreadStart ts = BeginWatch;
            RunningThread = new Thread(ts);
            _db = FileSyncDb.GetInstance(pathToWatch);
        }

        public void Start()
        {
            lock (this)
            {
                ShouldRun = true;
            }

            RunningThread.Start();
        }

        public void Stop()
        {
            lock (this)
            {
                ShouldRun = false;
                _watcher.Changed -= OnChanged;
                _watcher.Created -= OnChanged;
                _watcher.Deleted -= OnChanged;
                _watcher.Renamed -= OnChanged;
            }
        }

        public async Task<bool> ScanForUpdates()
        {
            var files = Directory.EnumerateFiles(PathToWatch, "*.*", SearchOption.AllDirectories).AsParallel();
            List<Task<bool>> tasksToAwait = new List<Task<bool>>();
            int maxTasksToAwait = 100;
            int runningTaskCounter = 0;
            foreach(var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                FsFile file = new FsFile() {LastModified = fileInfo.LastWriteTimeUtc, Path = FullToRelative(PathToWatch, fileInfo.FullName), Size = fileInfo.Length };

                if(runningTaskCounter < maxTasksToAwait)
                {
                    _ = Task.Run(async () => {
                        runningTaskCounter++;
                        await _db.Files.AddOrUpdate(file);
                        runningTaskCounter--;
                    });
                }
                else
                {
                    await _db.Files.AddOrUpdate(file);
                }
            }
            while(runningTaskCounter > 0)
            {
                Thread.Sleep(10);
            }
            return true;
        }

        //based on code from https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netcore-3.1
        private void BeginWatch()
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

            // Wait for the user to quit the program.
            while (ShouldRun == true) ;
            _watcher.Dispose();
        }

        public static string FullToRelative(string basePath, string fullPath)
        {
            return fullPath.Substring(basePath.Length + 1);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);
            FsFile file = new FsFile() { LastModified = fileInfo.LastWriteTimeUtc, Path = FullToRelative(PathToWatch, fileInfo.FullName), Size = fileInfo.Length };
            _db.Files.AddOrUpdate(file).Wait();
            FsFileSystemEventArgs args = new FsFileSystemEventArgs();
            args.BaseArgs = e;
            args.RelativePath = FullToRelative(PathToWatch, fileInfo.FullName);
            FileChangeDetected(this, args);
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
        }

    }
}
