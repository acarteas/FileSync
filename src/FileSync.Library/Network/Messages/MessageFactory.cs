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

            //skim off first int then rewind
            MessageIdentifier id = (MessageIdentifier)IPAddress.NetworkToHostOrder(reader.ReadInt32());
            reader.BaseStream.Seek(-sizeof(int), SeekOrigin.Current);

            switch (id)
            {
                case MessageIdentifier.FileChanged:
                    result = new FileChangedMessage(reader);
                    break;

                case MessageIdentifier.Verification:
                    result = new VerificationMessage(reader);
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
