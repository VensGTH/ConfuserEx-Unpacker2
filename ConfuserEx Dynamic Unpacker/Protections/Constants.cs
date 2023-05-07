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
        /**
        determines whether we have to call constructor of the type
        before invoking decryption method
        */
        private static bool m_CallConstructorBeforeInvoke = true;

        public static int constants()
        {
            int amount = 0;
            var manifestModule = Program.asm.ManifestModule;

            foreach (TypeDef types in Program.module.GetTypes())
            {
                bool bConstucted = true;
                foreach (MethodDef methods in types.Methods)
                {
                    if (!methods.HasBody)
                        continue;
                    for (int i = 0; i < methods.Body.Instructions.Count; i++)
                    {
                        if (methods.Body.Instructions[i].OpCode == OpCodes.Call &&
                            methods.Body.Instructions[i].Operand.ToString().Contains("tring>") &&
                            methods.Body.Instructions[i].Operand is MethodSpec)
                        {
                            if (i < 1)
                                continue;
                            if (methods.Body.Instructions[i - 1].IsLdcI4())
                            {
                                if (Program.veryVerbose)
                                {
                                    Console.Write("processing {0:X} {1:D}", methods.MDToken.ToInt32(),
                                                  methods.Name.Length);
                                }
                                MethodSpec methodSpec = methods.Body.Instructions[i].Operand as MethodSpec;
                                uint param1 = (uint)methods.Body.Instructions[i - 1].GetLdcI4Value();
                                MethodBase DecryptionMethod =
                                    manifestModule.ResolveMethod(methodSpec.MDToken.ToInt32());
                                if (!bConstucted)
                                {
                                    bConstucted = true;
                                    var ctor = types.FindMethod(".cctor");
                                    if (ctor == null)
                                    {
                                        Console.Write("constructor not found for type {0:X}", types.MDToken.ToInt32());
                                    }
                                    else
                                    {
                                        if (Program.veryVerbose)
                                        {
                                            Console.Write("found constructor {0:X}", ctor.MDToken.ToInt32());
                                            MethodBase ctor_method =
                                                manifestModule.ResolveMethod(ctor.MDToken.ToInt32());
                                            var g = new object[ctor.GetParamCount()];
                                            if (g.Length != 0)
                                                Console.Write("constructor requires params");
                                            object r = ctor_method.Invoke(null, g);
                                        }
                                    }
                                }
                                var value = DecryptionMethod.Invoke(null, new object[] { (uint)param1 });
                                if (value == null)
                                    continue;
                                // Console.Write("value={0}, param1={1}", value, param1.ToString());
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
