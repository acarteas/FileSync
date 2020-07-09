using System;
using System.Threading;

namespace FileSync.Testing
{
    class Program
    {
        static void RunNetworkTests()
        {
            NetworkTests tests = new NetworkTests();
            tests.Run();
        }

        static void RunFileSystemTests()
        {
            FileSystemTests tests = new FileSystemTests();
            tests.Run();
        }

        static void Main(string[] args)
        {
            RunNetworkTests();
            //RunFileSystemTests();
        }
    }
}
