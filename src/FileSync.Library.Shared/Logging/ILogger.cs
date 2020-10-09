using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Shared.Logging
{
    public enum LogPriority { Low, Medium, High };
    public interface ILogger
    {
        void Log(string message);
        void Log(string message, params object[] args);
        void Log(string message, LogPriority severity);
        void Log(LogPriority severity, string message, params object[] args);
    }
}
