﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public enum MessageType : byte
    { 
        Null,
        Get,
        Put,
        List
    }

}
