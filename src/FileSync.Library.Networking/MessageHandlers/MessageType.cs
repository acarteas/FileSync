using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public enum MessageType
    { 
        Error = -1,
        Null,
        Get,
        Put,
        List
    }

}
