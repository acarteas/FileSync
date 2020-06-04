using FileSync.Library.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Threading;

namespace FileSync.Terminal
{
    class Program
    {
        static void RunPublisher()
        {
            Publisher pub = new Publisher();
        }

        static void RunSubscriber()
        {
            Subscriber sub = new Subscriber();
            sub.Listen();
        }

        static void Main(string[] args)
        {
            //Run();
            ThreadStart subscriberFunction = RunSubscriber;
            Thread subscriber = new Thread(subscriberFunction);
            subscriber.Start();
            Thread.Sleep(1000);
            Publisher pub = new Publisher();
            pub.Send("hello, world!");
            subscriber.Join();
            Console.WriteLine("Done");
        }


        //sample code from https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netcore-3.1
        private static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();

            // If a directory is not specified, exit program.
            if (args.Length != 2)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: Watcher.exe (directory)");
                //return;
            }

            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = ".";//args[1];

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;

                // Only watch text files.
                //watcher.Filter = "*.txt";

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                Console.WriteLine("Press 'q' to quit the sample.");
                while (Console.Read() != 'q') ;
            }
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e) =>
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");

        private static void OnRenamed(object source, RenamedEventArgs e) =>
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
    }
}
