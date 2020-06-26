using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.FileSystem
{
    public class FsFile
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}
