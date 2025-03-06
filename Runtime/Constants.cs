using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AsStrongAsFuck.Runtime
{
    public class Constants
    {
        public static byte[] array = new byte[] { };
        static string key = "";

        static byte[] aes(byte[] data)
        {
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = Encoding.GetEncoding("ISO-8859-1").GetBytes(key);
            rDel.IV = Encoding.GetEncoding("ISO-8859-1").GetBytes(key);
            rDel.Mode = CipherMode.CBC;
            rDel.Padding = PaddingMode.PKCS7;
            data = rDel.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
            return data;
        }
        public static string Get(string one, int key, int len)
        {

            StackTrace trace = new StackTrace();
            var data = Encoding.Default.GetBytes(trace.GetFrame(1).GetMethod().Name);
            const int p = 16777619;
            int hash = -2128831035;

            for (int i = 0; i < data.Length; i++)
                hash = (hash ^ data[i]) * p;

            hash += hash << 13;
            hash ^= hash >> 7;
            List<byte> shit = new List<byte>();
            key += hash;
            for (int i = 0; i < len; i++)
            {
                if (array == null)
                {
                    shit.Add(97);
                }
                else
                {
                    shit.Add(array[key + i]);
                }
            }
            return Encoding.UTF8.GetString(shit.ToArray());
        }

        public static byte[] Initialize(int len)
        {

            try
            {
                byte[] myArray = new byte[len];
                //myArray[15] = 12;
                //myArray[16] = 13;

                return aes(myArray);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"init error: {ex.Message}");

            }
            return new byte[len];
        }

        public static void Set()
        {
            array[0] = 0;
        }

    }
}
