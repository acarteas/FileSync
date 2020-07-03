using System;
using System.Collections.Generic;
using System.Text;

namespace FileSync.Testing
{
    public class Assert
    {
        public static void AreEqual(IComparable item1, IComparable item2, string message = "", params object[] args)
        {
            if(item1.CompareTo(item2) != 0)
            {
                message = String.Format(message, args);
                throw new Exception(message);
            }
        }

        public static void AreEqual(byte[] item1, byte[] item2, string message = "", params object[] args)
        {
            message = String.Format(message, args);
            if (item1.Length != item2.Length)
            {
                throw new Exception(message);
            }
            for(int i = 0; i < item1.Length; i++)
            {
                for(int j = 0; j < item2.Length; j++)
                {
                    if(item1[i] != item2[j])
                    {
                        throw new Exception(message);
                    }
                }
            }
        }

        public static void Fail(string message = "", params object[] args)
        {
            message = String.Format(message, args);
            throw new Exception(message);
        }

        public static void IsTrue(bool item1, string message = "", params object[] args)
        {
            if(item1 != true)
            {
                message = String.Format(message, args);
                throw new Exception(message);
            }
        }
    }
}
