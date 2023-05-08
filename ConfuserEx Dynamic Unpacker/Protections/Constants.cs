using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    class Constants
    {

        public static int constants(ModuleDefMD module, IList<TypeDef> typesall)
        {
            int amount = 0;

            foreach (TypeDef types in /*Program.module.GetTypes()*/ typesall)
            {
                // subclasses yet again:
                if (types.NestedTypes.Count > 0)
                    amount += constants(module, types.NestedTypes);

                foreach (MethodDef methods in types.Methods)
                {
                    if (!methods.HasBody)
                        continue;
                    for (int i = 0; i < methods.Body.Instructions.Count; i++)
                    {
                        if (methods.Body.Instructions[i].OpCode == OpCodes.Call &&
                            methods.Body.Instructions[i].Operand.ToString().Contains("tring>") &&
                            methods.Body.Instructions[i].Operand.ToString().Contains("odule>") &&
                            methods.Body.Instructions[i].Operand is MethodSpec)
                        {
                            if (i < 1)
                                continue;
                            if (methods.Body.Instructions[i - 1].IsLdcI4())
                            {
                                if (Program.veryVerbose)
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine("processing {0:X}:{1:D}" /* +
                                                           methods.Body.Instructions[i].Operand.ToString() */
                                                      ,
                                                      methods.MDToken.ToInt32(), i);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                                MethodSpec methodSpec = methods.Body.Instructions[i].Operand as MethodSpec;
                                uint param1 = (uint)methods.Body.Instructions[i - 1].GetLdcI4Value();
                                MethodBase DecryptionMethod =
                                    Program.asm.ManifestModule.ResolveMethod(methodSpec.MDToken.ToInt32());
                                var value = DecryptionMethod.Invoke(null, new object[] { (uint)param1 });
                                if (value == null)
                                    continue;
                                methods.Body.Instructions[i].OpCode = OpCodes.Nop;
                                methods.Body.Instructions[i - 1].OpCode = OpCodes.Ldstr;
                                methods.Body.Instructions[i - 1].Operand = value;
                                amount++;
                                if (Program.veryVerbose)
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format(
                                        "Encrypted String Found In Method {0:X} With Param of {1} the decrypted string is {2}",
                                        methods.MDToken.ToInt32(), param1.ToString(), value));
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                            }
                        }
                    }
                }
            }
            return amount;
        }
    }
}
