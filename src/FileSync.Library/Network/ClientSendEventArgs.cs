using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network
{
    public class ClientSendEventArgs : EventArgs
    {
        public FileMetaData FileData { get; set; }
        public bool WasSuccessful { get; set; }
    }
}
