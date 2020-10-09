using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;

namespace FileSync.Library.Networking.MessageHandlers
{
    public class MessageHandlerFactory
    {
        public string AuthKey { get; set; }
        public ServerConfig Config { get; set; }

        public IMessageHandler CreateMessageHander(MessageType type)
        {
            IMessageHandler result = null;
            switch (type)
            {
                case MessageType.Get:
                    result = new GetMessageHandler() { AuthKey = AuthKey, Config = Config}; 
                    break;

                case MessageType.Put:
                    result = new PutMessageHandler() { AuthKey = AuthKey, Config = Config };
                    break;

                default:
                    result = new NullMessageHandler();
                    break;
            }
            return result;
        }
    }
}
