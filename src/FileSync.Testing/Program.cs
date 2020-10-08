using System;
using System.Threading;

namespace FileSync.Testing
{
    class Program
    {
        static void RunNetworkTests()
        {
            NetworkTests tests = new NetworkTests();
            //tests.Run();
        }

        static void RunFileSystemTests()
        {
            FileSystemTests tests = new FileSystemTests();
            tests.Run();
        }

        static void Main(string[] args)
        {
            //Watcher watcher = new Watcher("Z:/music");
            //watcher.ScanForFiles();
            //Console.WriteLine("Done testing.");
            //RunNetworkTests();
            RunFileSystemTests();
        }
    }
}
