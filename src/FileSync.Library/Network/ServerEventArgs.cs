using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network
{
    public class ServerEventArgs : EventArgs
    {
        public FileMetaData FileData { get; set; }
        public string FullLocalPath { get; set; }
    }
}
