using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Config
{
    public class Connection
    {
        public string Nickname { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string AccessKey { get; set; }
        public string LocalSyncPath { get; set; }
        public List<string> DirectoriesToSync { get; set; }

        /// <summary>
        /// Acts as a cache for list of remote directories to sync
        /// </summary>
        private Dictionary<string, int> _directoriesToSync = new Dictionary<string, int>();
        public Connection()
        {
            DirectoriesToSync = new List<string>();
        }

        public bool IsRemoteSyncDirectory(string directory)
        {
            if(_directoriesToSync.ContainsKey(directory) == true)
            {
                return true;
            }
            if(DirectoriesToSync.Contains(directory) == true)
            {
                _directoriesToSync.Add(directory, 1);
                return true;
            }
            return false;
        }
    }
}
