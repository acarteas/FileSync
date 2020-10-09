
using FileSync.Library.Networking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Threading;

namespace FileSync.Terminal
{
    class Program
    {
        static Thread serverThread = null;
        static Thread clientThread = null;
        static int listenPort = 13000;
        
        
        static void LoadConfig()
        {
            //string configText = File.ReadAllText("config.json");
            //config = JsonConvert.DeserializeObject<FileSyncConfig>(configText);
        }


        static void Main(string[] args)
        {
            ServerConfig config = new ServerConfig();
            config.ListenPort = 13000;
            config.Shares.Add("temp", new ServerShare());
            config.Shares["temp"].Clients.Add("ABC123", "127.0.0.1");
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText("server_config.json", json);
            Console.WriteLine("Ending program");
        }

    }


        
}
