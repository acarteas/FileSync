using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.FileSystem.Database.Models
{
    public class FsFile
    {
        public int Id { get; set; }

        /// <summary>
        /// The path of the file relative to the base share directory
        /// </summary>
        public string Path { get; set; }
        public long Size { get; set; }

        public DateTime LastModified
        {
            get
            {
                return new DateTime(Ticks, DateTimeKind.Utc);
            }
            set
            {
                Ticks = value.Ticks;
            }
        }
        public long Ticks { get; set; }
        public bool IsDeleted { get; set; }
    }
}
