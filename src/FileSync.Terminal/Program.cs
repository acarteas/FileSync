using FileSync.Library.FileSystem;
using FileSync.Library.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            List<Watcher> watchers = new List<Watcher>();
            watchers.Add(new Watcher());

            Console.WriteLine("Listening for changes to file system");
            foreach(Watcher w in watchers)
            {
                w.FileChangeDetected += HandleFileChange;
                w.Start();
            }

            //loop for as long as at least on watcher is active
            bool keepGoing = true;
            while(keepGoing == true)
            {
                keepGoing = false;
                foreach(Watcher w in watchers)
                {
                    if(w.RunningThread.IsAlive == true)
                    {
                        keepGoing = true;
                    }
                }
            }
            Console.WriteLine("Ending program");
        }

        private static void HandleFileChange(object sender, FileSystemEventArgs e)
        {
            using(BinaryReader reader = new BinaryReader(File.OpenRead(e.FullPath)))
            {
                reader.
            }
            Console.WriteLine("detected {0} at {1}", e.ChangeType.ToString(), e.Name);
        }
    }


        
}
