﻿using System;
using System.Threading;

namespace FileSync.Testing
{
    class Program
    {
        static void RunFileSystemTests()
        {
            FileSystemTests tests = new FileSystemTests();
            tests.Init();

            Console.WriteLine("Running file system create test...");
            tests.Create();
            Thread.Sleep(100);
            tests.Wait();

            Console.WriteLine("Running file system update test...");
            tests.UpdateTest();
            Thread.Sleep(100);
            tests.Wait();

            Console.WriteLine("Running file system rename test...");
            tests.RenameTest();
            Thread.Sleep(100);
            tests.Wait();

            Console.WriteLine("Running file system delete test...");
            tests.DeleteTest();
            Thread.Sleep(100);
            tests.Wait();

            tests.Teardown();
        }

        static void Main(string[] args)
        {
            RunFileSystemTests();
        }
    }
}
