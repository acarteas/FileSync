using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public enum MessageIdentifier
    {
        Null = 0,
        Verification,
        FileChanged
    }
}
