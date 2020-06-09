using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FileSync.Library.FileSystem
{
    public class Watcher
    {
        public event EventHandler<FileSystemEventArgs> FileChangeDetected = delegate { };
        public string PathToWatch { get; protected set; }
        protected bool ShouldRun { get; set; }
        public Thread RunningThread { get; protected set; }
        public Watcher(string pathToWatch = ".")
        {
            PathToWatch = pathToWatch;
            ShouldRun = true;
            ThreadStart ts = BeginWatch;
            RunningThread = new Thread(ts);
        }

        public void Start()
        {
            lock(this)
            {
                ShouldRun = true;
            }

            RunningThread.Start();
        }

        public void Stop()
        {
            lock(this)
            {
                ShouldRun = false;
            }
        }

        //based on code from https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netcore-3.1
        private void BeginWatch()
        {

            // Create a new FileSystemWatcher and set its properties.
            using (System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher())
            {
                watcher.Path = PathToWatch;
                watcher.IncludeSubdirectories = true;

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;

                // Example if we wanted to restrict file types
                //watcher.Filter = "*.txt";

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnChanged;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                while (ShouldRun == true);
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FileChangeDetected(this, e);
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
        }
            
    }
}
