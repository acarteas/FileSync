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
        public List<string> RemoteSyncDirectories { get; set; }

        /// <summary>
        /// Acts as a cache for list of remote directories to sync
        /// </summary>
        private Dictionary<string, int> _remoteSyncDirectoriesDict = new Dictionary<string, int>();
        public Connection()
        {
            RemoteSyncDirectories = new List<string>();
        }

        public bool IsRemoteSyncDirectory(string directory)
        {
            if(_remoteSyncDirectoriesDict.ContainsKey(directory) == true)
            {
                return true;
            }
            if(RemoteSyncDirectories.Contains(directory) == true)
            {
                _remoteSyncDirectoriesDict.Add(directory, 1);
                return true;
            }
            return false;
        }
    }
}
