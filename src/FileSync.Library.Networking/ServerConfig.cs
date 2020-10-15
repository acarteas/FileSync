using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSync.Library.Networking
{
    public class ServerConfig
    {
        public int ListenPort { get; set; }
        public bool BindKeyToIpAddress { get; set; }
        public Dictionary<string, ServerShare> Shares { get; set; }
        public int MaxApiKeyLength { get; set; }


        public ServerConfig()
        {
            BindKeyToIpAddress = false;
            Shares = new Dictionary<string, ServerShare>();
        }

        /// <summary>
        /// Returns the fully qualified local file path for the given share / path combination
        /// </summary>
        /// <param name="shareName">The base name of the share (e.g. Music)</param>
        /// <param name="relativePath">The relative path of the file requested (e.g. Beethoven/9th.mp3)</param>
        /// <returns>The fully qualaified local file path to the music (e.g. e:/shares/music/Beethoven/9th.mp3)</returns>
        public string GetFilePath(string shareName, string relativePath)
        {
            return Path.Combine(Shares[shareName].AbsolutePath, relativePath);
        }

        /// <summary>
        /// Determines whether or not the access key is allowed to access the specified file share
        /// </summary>
        /// <param name="key">Authentication key</param>
        /// <param name="path">Path of share</param>
        /// <returns></returns>
        public bool HasShareAccess(string key, string share)
        {
            return true;
        }

        public bool IsValidKey(string key, string address)
        {
            return true;
            //return ValidKeys.ContainsKey(key);
        }
    }
}
