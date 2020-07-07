using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class MessageFactory
    {
        public static IMessage FromStream(BinaryReader reader)
        {
            IMessage result = null;

            //skim off first int.  Note that this makes IMessage's From* and To* methods asymmetric.  I don't 
            //really like that but for now it's good enough.
            MessageIdentifier id = (MessageIdentifier)IPAddress.NetworkToHostOrder(reader.ReadInt32());

            switch (id)
            {
                case MessageIdentifier.FileChanged:
                    result = new FileChangedMessage(reader);
                    break;

                case MessageIdentifier.FileRequest:
                    result = new FileRequestMessage(reader);
                    break;

                case MessageIdentifier.FileData:
                    result = new FileDataMessage(reader);
                    break;

                case MessageIdentifier.Verification:
                    result = new VerificationMessage(reader);
                    break;

                case MessageIdentifier.GetUpdates:
                    result = new GetUpdatesMessage(reader);
                    break;

                case MessageIdentifier.Null:
                default:
                    result = new NullMessage(reader);
                    break;
            }

            return result;
        }
    }
}
