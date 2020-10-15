using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FileSync.Library.Shared
{
    public class ApiKey
    {
        public static byte[] GenerateNewKeyBytes()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
            }
            return key;
        }


        public static string GenerateNewKey()
        {
            return Convert.ToBase64String(GenerateNewKeyBytes());
        }
    }
}
