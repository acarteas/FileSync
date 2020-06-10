using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FileSync.Library.Logging
{
    /// <summary>
    /// Writes log information to debug console
    /// </summary>
    public class DebugLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.WriteLine(message);
        }

        public void Log(string message, params object[] args)
        {
            Debug.WriteLine(message, args);
        }
    }
}
