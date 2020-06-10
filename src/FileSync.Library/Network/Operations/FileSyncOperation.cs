using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public enum FileSyncOperation
    {
        //no-op
        Null = 0,

        //A network call that contains a full file
        File,

        //A network call that has information for a given file
        FileInfo,

        //A network call in which the caller is requesting all recent changes
        //logged by the server.  
        GetUpdates
    }
}
