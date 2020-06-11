using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public enum FileSyncOperation
    {
        //no-op
        Null = 0,

        //A network request in which the sender would like to send a file
        SendFile,

        //A network call in which the sender is requesting all recent changes.  
        GetUpdates
    }
}
