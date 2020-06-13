using FileSync.Library.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network
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
        private string LastWriteTimeUtcString = "";

        private DateTime _lastWriteTimeUtc;
        public DateTime LastWriteTimeUtc
        {
            get
            {
                return _lastWriteTimeUtc;
            }
            set
            {
                _lastWriteTimeUtc = value;
                LastWriteTimeUtcString = _lastWriteTimeUtc.ToString(Constants.TimeFormatString);
            }
        }

        [JsonProperty]
        private string LastAccessTimeUtcString = "";

        private DateTime _lastAccessTimeUtc;
        public DateTime LastAccessTimeUtc
        {
            get
            {
                return _lastAccessTimeUtc;
            }
            set
            {
                _lastAccessTimeUtc = value;
                LastAccessTimeUtcString = _lastAccessTimeUtc.ToString(Constants.TimeFormatString);
            }
        }

        [JsonProperty]
        private string CreateTimeUtcString = "";

        private DateTime _createTimeUtc;
        public DateTime CreateTimeUtc
        {
            get
            {
                return _createTimeUtc;
            }
            set
            {
                _createTimeUtc = value;
                CreateTimeUtcString = _createTimeUtc.ToString(Constants.TimeFormatString);
            }
        }

        public FileMetaData()
        {
            LastWriteTimeUtc = DateTime.MinValue.ToUniversalTime();
            LastAccessTimeUtc = DateTime.MinValue.ToUniversalTime();
            CreateTimeUtc = DateTime.MinValue.ToUniversalTime();
        }

        public static FileMetaData FromBytes(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<FileMetaData>(json);
            
        }

        public byte[] ToBytes()
        {
            var thisAsString = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(thisAsString);
        }
    }
}
