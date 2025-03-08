using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace AsStrongAsFuck
{
    public class RenameObfuscation : IObfuscation
    {
        public void Execute(ModuleDefMD md)
        {
            foreach (var type in md.Types)
            {
                if (type.Name.Equals("<Module>"))
                {
                    continue;
                }
                foreach (var method in type.Methods.Where(x => !x.IsConstructor && !x.IsVirtual && !x.IsPublic))
                    Renamer.Rename(method, Renamer.RenameMode.Logical, 3);
            }
        }
    }
}
