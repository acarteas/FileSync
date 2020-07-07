using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FileSync.Library.Network.Messages
{
    public class GetUpdatesMessage : IMessage
    {
        public MessageIdentifier MessageId { get { return MessageIdentifier.GetUpdates; } }

        /// <summary>
        /// The starting time in UTC ticks from which the sender would like file udpates
        /// </summary>
        public long DateTicks { get; set; }

        /// <summary>
        /// The number of files contained in this message
        /// </summary>
        public int FileCout { get; set; }

        /// <summary>
        /// An array of updated files
        /// </summary>
        public FileMetaData[] Files { get; set; }

        public GetUpdatesMessage(BinaryReader reader)
        {
            FromBinaryStream(reader);
        }
        public GetUpdatesMessage(DateTime dt)
        {
            DateTicks = dt.ToUniversalTime().Ticks;
        }


        public void FromBinaryStream(BinaryReader reader)
        {
            DateTicks = IPAddress.NetworkToHostOrder(reader.ReadInt64());
            FileCout = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            if(FileCout > 0)
            {
                Files = new FileMetaData[FileCout];
            }
            for(int i = 0; i < FileCout; i++)
            {
                int numBytes = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                Files[i] = FileMetaData.FromBytes(reader.ReadBytes(numBytes));
            }
        }

        public void FromBytes(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    FromBinaryStream(reader);
                }
            }
        }

        public byte[] ToBytes()
        {
            byte[] result = null;

            using(MemoryStream ms = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((int)MessageId));
                    writer.Write(IPAddress.HostToNetworkOrder(DateTicks));
                    writer.Write(IPAddress.HostToNetworkOrder(FileCout));
                    for(int i = 0; i < FileCout; i++)
                    {
                        byte[] fdBytes = Files[i].ToBytes();
                        writer.Write(IPAddress.HostToNetworkOrder(fdBytes.Length));
                        writer.Write(fdBytes);
                    }
                }
            }

            return result;
        }
    }
}
