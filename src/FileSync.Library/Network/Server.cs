﻿using FileSync.Library.Config;
using FileSync.Library.Logging;
using FileSync.Library.Network.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

//expected stream format: 
//INT-32 (length of auth key)
//BYTE[] (auth key)
//AUTH-KEY (512 bytes)
//INT-32 (length of file name)
//STRING-UTF8 (file name)
//INT-32 (length relative path location)
//STRING-UTF8 (relative path location)
//BYTE[] (binary file data)
namespace FileSync.Library.Network
{
    //TODO: add option for SSL communication (tutorial at https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1)
    public class Server
    {
        private static int _thread_counter = 1;
        private int _thread_id;
        public ILogger Logger { get; set; }
        protected FileSyncConfig _config;

        public TcpListener Listener { get; protected set; }
        public Server(FileSyncConfig config, TcpListener listener, ILogger logger)
        {
            _config = config;
            Listener = listener;
            _thread_id = _thread_counter;
            _thread_counter++;
            Logger = logger;
        }

        public void Start()
        {
            Logger.Log("Server Thread #{0} waiting for connection...", _thread_id);
            var client = Listener.AcceptTcpClient();
            Logger.Log("Thread #{0} accepting client: {1}", _thread_id, client.Client.RemoteEndPoint);

            //reject connections not stored in our config
            string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            if(_config.RemoteConnections.ContainsKey(address) == true)
            {
                Connection activeConnection = _config.RemoteConnections[address];
                BufferedStream stream = new BufferedStream(client.GetStream());
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);
                try
                {
                    //verify client
                    var validator = new ReceiveValidationOperation(reader, writer, Logger, activeConnection);
                    bool isValidated = validator.Run();
                    if (isValidated == true)
                    {
                        //determine client intent
                        var opReader = new ReceiveFileSyncOperation(reader, writer, Logger);
                        opReader.Run();
                        FileSyncOperation op = opReader.Operation;

                        //build appropraite response based on intent
                        switch (op)
                        {
                            case FileSyncOperation.SendFile:

                                //grab metadata
                                var metaDataOperation = new ReceiveFileMetadataOperation(reader, writer, Logger);
                                metaDataOperation.Run();

                                //find location of file on our file system
                                string filePath = Path.Combine(activeConnection.LocalSyncPath, metaDataOperation.FileData.Path);

                                //handle rename operations separately
                                if (metaDataOperation.FileData.OperationType != WatcherChangeTypes.Renamed)
                                {
                                    FileInfo localFile = new FileInfo(filePath);

                                    //if our copy is older than theirs, take it
                                    if (localFile.Exists == false || localFile.LastWriteTimeUtc < metaDataOperation.FileData.LastWriteTimeUTC)
                                    {
                                        var grabFileOperation = new ReceiveFileOperation(reader,writer, Logger, filePath);
                                        grabFileOperation.Run();
                                    }
                                }
                                else
                                {
                                    //no need to send file over network if all we're doing is a rename
                                    string oldFilePath = Path.Combine(activeConnection.LocalSyncPath, metaDataOperation.FileData.OldPath);
                                    if (File.Exists(oldFilePath))
                                    {
                                        File.Move(oldFilePath, filePath);
                                    }
                                }

                                break;

                            case FileSyncOperation.GetUpdates:
                                break;

                            case FileSyncOperation.Null:
                            default:
                                break;
                        }

                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Thread #{0} exception: {1}", _thread_id, ex.Message);
                }
                finally
                {
                    reader.Close();
                    writer.Close();
                }
            }

            if(client.Connected == true)
            {
                client.Close();
            }
        }
    }
}
