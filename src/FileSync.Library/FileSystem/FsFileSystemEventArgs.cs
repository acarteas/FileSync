using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Library.FileSystem
{
    public class FsFileSystemEventArgs 
    {
        public FileSystemEventArgs BaseArgs { get; set; }
        public string RelativePath { get; set; }
    }
}
