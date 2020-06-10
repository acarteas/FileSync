using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Logging
{
    public interface ILogger
    {
        void Log(string message);
        void Log(string message, params object[] args);
    }
}
