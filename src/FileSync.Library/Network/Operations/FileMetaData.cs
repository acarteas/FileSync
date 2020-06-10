using FileSync.Library.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Library.Network.Operations
{
    public class FileMetaData
    {
        /// <summary>
        /// Will contain the path of the file relative to the base share.  
        /// E.g., if the file is located at c:/shares/music/song.mp3 and the base
        /// share path is c:/share, then Path would be music/song.mp3.
        /// </summary>
        public string Path { get; set; }
        public WatcherChangeTypes OperationType { get; set; }
        /// <summary>
        /// On a rename operation, will contain the old name of the file.  Otherwise, empty string.
        /// </summary>
        public string OldPath { get; set; }

        [JsonProperty]
        private string LastWriteTimeUTCString = "";

        private DateTime _lastWriteTimeUTC;
        public DateTime LastWriteTimeUTC
        {
            get
            {
                return _lastWriteTimeUTC;
            }
            set
            {
                _lastWriteTimeUTC = value;
                LastWriteTimeUTCString = _lastWriteTimeUTC.ToString(Constants.TimeFormatString);
            }
        }

        public FileMetaData()
        {
            _lastWriteTimeUTC = DateTime.MinValue.ToUniversalTime();
        }
    }
}
