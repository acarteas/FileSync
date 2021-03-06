﻿using System;

using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Logging
{
    /// <summary>
    /// Writes log messages to console window
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void Log(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        public void Log(string message, LogPriority severity)
        {
            Log(message);
        }
        public void Log(LogPriority severity, string message, params object[] args)
        {
            Log(message, args);
        }

    }
}
