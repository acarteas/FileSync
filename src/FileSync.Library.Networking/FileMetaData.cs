using FileSync.Library.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Networking
{
    public class FileMetaData
    {
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

        public long FileSize { get; set; }

        public FileMetaData()
        {
            LastWriteTimeUtc = DateTime.MinValue.ToUniversalTime();
            LastAccessTimeUtc = DateTime.MinValue.ToUniversalTime();
            CreateTimeUtc = DateTime.MinValue.ToUniversalTime();
        }

        public static FileMetaData FromReader(BinaryReader reader)
        {
            int length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            var jsonBytes = reader.ReadBytes(length);
            return JsonConvert.DeserializeObject<FileMetaData>(Encoding.UTF8.GetString(jsonBytes));
        }

        public static FileMetaData FromBytes(byte[] bytes)
        {
            using(MemoryStream ms = new MemoryStream(bytes))
            {
                using(BinaryReader reader = new BinaryReader(ms))
                {
                    return FromReader(reader);
                }
            }
        }

        public byte[] ToBytes()
        {
            byte[] result = null;
            var thisAsString = JsonConvert.SerializeObject(this);
            using(MemoryStream ms = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(ms))
                {
                    var bytes = Encoding.UTF8.GetBytes(thisAsString);
                    writer.Write(IPAddress.HostToNetworkOrder(bytes.Length));
                    writer.Write(bytes);
                    result = ms.ToArray();
                }
            }
            return result;
        }
    }
}
