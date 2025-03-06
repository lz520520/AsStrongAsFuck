using AsStrongAsFuck.Runtime;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AsStrongAsFuck
{
    public class ConstantsEncoding : IObfuscation
    {
        public int CurrentIndex { get; set; } = 0;
        public MethodDef Decryptor { get; set; }
        public List<byte> array = new List<byte>();
        public Dictionary<RVA, List<Tuple<int, int, int>>> Keys = new Dictionary<RVA, List<Tuple<int, int, int>>>();

        static void il(MethodDef def)
        {
            int index = 0;

            foreach (var instr in def.Body.Instructions)
            {
                Console.WriteLine($"{index}: {instr.OpCode} {instr.Operand}");
                index++;
            }
        }


        public void Execute(ModuleDefMD md)
        {
            //TypeDef globalType = new TypeDefUser("RuntimeConstants", md.CorLibTypes.Object.TypeDefOrRef);
            //globalType.Attributes = TypeAttributes.Public;

            var consttype = RuntimeHelper.GetRuntimeType("AsStrongAsFuck.Runtime.Constants");
            FieldDef arrayField = consttype.FindField("array");
            Renamer.Rename(arrayField, Renamer.RenameMode.Base64, 2);

            arrayField.DeclaringType = null;
            foreach (TypeDef type in md.Types)
                foreach (MethodDef method in type.Methods)
                    if (method.HasBody && method.Body.HasInstructions)
                        ExtractStrings(method);
            md.GlobalType.Fields.Add(arrayField);
            MethodDef todef = consttype.FindMethod("Get");
            Renamer.Rename(todef, Renamer.RenameMode.Base64, 2);
            todef.DeclaringType = null;
            todef.Body.Instructions[58].Operand = arrayField;
            todef.Body.Instructions[73].Operand = arrayField;
            //Renamer.Rename(todef, Renamer.RenameMode.Logical);
            md.GlobalType.Methods.Add(todef);
            MethodDef init = consttype.FindMethod("Initialize");
            Renamer.Rename(init, Renamer.RenameMode.Base64, 2);

            MethodDef aesDecode = consttype.FindMethod("aes");
            Renamer.Rename(aesDecode, Renamer.RenameMode.Base64, 2);

            aesDecode.DeclaringType = null;
            init.DeclaringType = null;
            init.Body.Instructions[6].Operand = aesDecode;
  
            string key = getRandomString(16);
            var compressed = aesEncode(array.ToArray(),key);

            FieldDef keyField = consttype.FindField("key");
            Renamer.Rename(keyField, Renamer.RenameMode.Base64, 2);

            keyField.DeclaringType = null;
            md.GlobalType.Fields.Add(keyField);
            aesDecode.Body.Instructions[6].Operand = keyField;
            aesDecode.Body.Instructions[13].Operand = keyField;
            md.GlobalType.Methods.Add(aesDecode);
            var locArray = init.Body.Instructions[4].Operand;

            for (int i = 0; i < compressed.Length; i++)
            {
                // 2. 赋值 array[15] = 158;
                int offset = 5;
                // 反向
                init.Body.Instructions.Insert(offset, new Instruction(OpCodes.Stelem_I1)); // array[15] = 158
                init.Body.Instructions.Insert(offset, new Instruction(OpCodes.Ldc_I4, Convert.ToInt32(compressed[i]))); // 要存储的值 158
                init.Body.Instructions.Insert(offset, new Instruction(OpCodes.Ldc_I4, i)); // 数组索引 15
                init.Body.Instructions.Insert(offset, new Instruction(OpCodes.Ldloc, locArray)); // 复制数组引用
            }
            md.GlobalType.Methods.Add(init);
            Decryptor = todef;

            MethodDef cctor = md.GlobalType.FindOrCreateStaticConstructor();

            cctor.Body = new CilBody();
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Ldstr, key));  // 加载 "key"
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Stsfld, keyField));  // 赋值给 key

            cctor.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, compressed.Length));
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Call, init));
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Stsfld, arrayField));
            cctor.Body.Instructions.Add(new Instruction(OpCodes.Ret));

            foreach (TypeDef type2 in md.Types)
                foreach (MethodDef method2 in type2.Methods)
                    if (method2.HasBody && method2.Body.HasInstructions)
                        ReferenceReplace(method2);



        }

        public void ReferenceReplace(MethodDef method)
        {
            method.Body.SimplifyBranches();
            if (Keys.ContainsKey(method.RVA))
            {
                List<Tuple<int, int, int>> keys = Keys[method.RVA];
                keys.Reverse();
                foreach (Tuple<int, int, int> v in keys)
                {
                    method.Body.Instructions[v.Item1].Operand = "";
                    method.Body.Instructions.Insert(v.Item1 + 1, new Instruction(OpCodes.Ldc_I4, v.Item2));
                    method.Body.Instructions.Insert(v.Item1 + 2, new Instruction(OpCodes.Ldc_I4, v.Item3));
                    method.Body.Instructions.Insert(v.Item1 + 3, new Instruction(OpCodes.Call, Decryptor));
                }
            }
            method.Body.OptimizeBranches();
        }

        public void ExtractStrings(MethodDef method)
        {
            List<Tuple<int, int, int>> shit = new List<Tuple<int, int, int>>();
            foreach (Instruction instr in method.Body.Instructions)
            {
                bool flag = instr.OpCode == OpCodes.Ldstr;
                if (flag)
                {
                    string code = (string)instr.Operand;
                    if (code.StartsWith("{{"))
                    {
                        continue;
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes(code);
                    foreach (byte v in bytes)
                    {
                        array.Add(v);
                    }
                    var curname = Encoding.Default.GetBytes(method.Name);

                    const int p = 16777619;
                    int hash = -2128831035;

                    for (int i = 0; i < curname.Length; i++)
                        hash = (hash ^ curname[i]) * p;

                    hash += hash << 13;
                    hash ^= hash >> 7;

                    shit.Add(new Tuple<int, int, int>(method.Body.Instructions.IndexOf(instr), CurrentIndex - hash, bytes.Length));
                    CurrentIndex += bytes.Length;
                }
            }
            if (!Keys.ContainsKey(method.RVA))
                Keys.Add(method.RVA, shit);
        }
        static string getRandomString(int length)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
            char[] randomString = new char[length];

            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                randomString[i] = chars[random.Next(chars.Length)];
            }

            return new string(randomString);
        }
        public static byte[] aesEncode(byte[] data, string key)
        {
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = Encoding.GetEncoding("ISO-8859-1").GetBytes(key);
            rDel.IV = Encoding.GetEncoding("ISO-8859-1").GetBytes(key);
            rDel.Mode = CipherMode.CBC;
            rDel.Padding = PaddingMode.PKCS7;

            return rDel.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress);
            ds.Write(data, 0, data.Length);
            ds.Flush();
            ds.Close();
            return ms.ToArray();
        }
        public static byte[] Decompress(byte[] data)
        {
            const int BUFFER_SIZE = 256;
            byte[] tempArray = new byte[BUFFER_SIZE];
            List<byte[]> tempList = new List<byte[]>();
            int count = 0, length = 0;
            MemoryStream ms = new MemoryStream(data);
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);
            while ((count = ds.Read(tempArray, 0, BUFFER_SIZE)) > 0)
            {
                if (count == BUFFER_SIZE)
                {
                    tempList.Add(tempArray);
                    tempArray = new byte[BUFFER_SIZE];
                }
                else
                {
                    byte[] temp = new byte[count];
                    Array.Copy(tempArray, 0, temp, 0, count);
                    tempList.Add(temp);
                }
                length += count;
            }
            byte[] retVal = new byte[length];
            count = 0;
            foreach (byte[] temp in tempList)
            {
                Array.Copy(temp, 0, retVal, count, temp.Length);
                count += temp.Length;
            }
            return retVal;
        }

    }
}