using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck
{
    public static class Extensions
    {
        public static string Base64Representation(this string str)
        {
            var data = Convert.ToBase64String(Encoding.Default.GetBytes(str)).TrimEnd('=');
            //Console.WriteLine($"origin: {str}, new: {data}");
            return data;
        }
    }
}
