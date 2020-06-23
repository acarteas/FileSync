using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Library.Network
{
    public class SendQueue
    {
        private Dictionary<string, FileMetaData> _queueItems = new Dictionary<string, FileMetaData>();
        private Queue<FileMetaData> _queue = new Queue<FileMetaData>();

        public SendQueue()
        {

        }

        public bool IsEmpty()
        {
            return _queue.Count == 0;
        }

        public bool Enqueue(FileMetaData data)
        {
            if(data == null || data.Path.Length == 0)
            {
                return false;
            }
            if(_queueItems.ContainsKey(data.Path))
            {
                return false;
            }
            _queueItems.Add(data.Path, data);
            _queue.Enqueue(data); 
            return true;
        }

        public FileMetaData Dequeue()
        {
            FileMetaData front = _queue.Dequeue();
            _queueItems.Remove(front.Path);
            return front;
        }

        public FileMetaData Front()
        {
            return _queue.Peek();
        }
    }
}
