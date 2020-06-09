using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network
{
    public class ClientEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public bool WasSuccessful { get; set; }
    }
}
