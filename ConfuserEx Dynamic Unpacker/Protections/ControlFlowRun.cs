using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    class ControlFlowRun
    {
        private static BlocksCflowDeobfuscator CfDeob;

        public static void DeobfuscateCflow(MethodDef meth)
        {
            for (int i = 0; i < 2; i++)
            {
                CfDeob = new BlocksCflowDeobfuscator();
                Blocks blocks = new Blocks(meth);
                List<Block> test = blocks.MethodBlocks.GetAllBlocks();
                blocks.RemoveDeadBlocks();
                blocks.RepartitionBlocks();

                blocks.UpdateBlocks();
                blocks.Method.Body.SimplifyBranches();
                blocks.Method.Body.OptimizeBranches();
                CfDeob.Initialize(blocks);
                // CfDeob.Deobfuscate();
                CfDeob.Add(new ControlFlow());

                // CfDeob.Add(new Cflow());
                CfDeob.Deobfuscate();
                blocks.RepartitionBlocks();

                IList<Instruction> instructions;
                IList<ExceptionHandler> exceptionHandlers;
                blocks.GetCode(out instructions, out exceptionHandlers);
                DotNetUtils.RestoreBody(meth, instructions, exceptionHandlers);
            }
        }
        static void stripCallCheck(Block block)
        {
            var instrs = block.Instructions;
            for (int i = 0; i < instrs.Count; i++)
            {
                var instr = instrs[i];
                if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
                {
                    var calledMethod = (IMethod)instr.Operand;
                    if (calledMethod.DeclaringType.DefinitionAssembly.IsCorLib())
                    {
                        var calledMethodFullName = calledMethod.FullName;
                        // debug:Console.WriteLine(calledMethodFullName);
                        if (calledMethodFullName ==
                            "System.Diagnostics.StackFrame System.Diagnostics.StackTrace::GetFrame(System.Int32)")
                        {
                            i += 1;
                            if ((instrs.Count - i) < 4)
                                continue;
                            var instnext = instrs[i];
                            calledMethodFullName = ((IMethod)instnext.Operand).FullName;
                            if (calledMethodFullName ==
                                "System.Reflection.MethodBase System.Diagnostics.StackFrame::GetMethod()")
                            {
                                i += 2; // skip get_DeclaringType
                                instnext = instrs[i];
                                if (instnext.OpCode.Code == Code.Ldtoken)
                                {
                                    // check if already patched:
                                    if (instrs[i + 1].OpCode == OpCodes.Nop)
                                        continue;
                                    if (Program.veryVerbose)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine("Replace StackTrace::GetFrame");
                                        Console.ForegroundColor = ConsoleColor.Green;
                                    }
                                    block.Replace(i, 1, Instruction.CreateLdcI4(777));
                                    block.Replace(i + 1, 1, OpCodes.Nop.ToInstruction());
                                    i += 2;
                                }
                            }
                            continue;
                        }
                        else if (calledMethodFullName ==
                                 "System.Reflection.Assembly System.Reflection.Assembly::GetCallingAssembly()")
                        {
                            i += 1;
                            if ((instrs.Count - i) < 1)
                                continue;
                            var instnext = instrs[i];
                            calledMethodFullName = ((IMethod)instnext.Operand).FullName;
                            if (calledMethodFullName ==
                                "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()")
                            {
                                if (Program.veryVerbose)
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine("Replacing GetExecutingAssembly");
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                                block.Replace(i, 1, instr.Instruction.Clone());
                                i += 1;
                            }
                            continue;
                        }
                    }
                }
            }
        } //..stripCallCheck

        public static bool stripCallChecks(dnlib.DotNet.MethodDef methodDef)
        {
            if (Program.veryVerbose)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Removing call checks for {0:X}", methodDef.MDToken.ToInt32());
                Console.ForegroundColor = ConsoleColor.Green;
            }
            var blocks = new Blocks(methodDef);
            foreach (var block in blocks.MethodBlocks.GetAllBlocks())
                stripCallCheck(block);
            // blocks.UpdateBlocks(); // ??
            IList<Instruction> instructions;
            IList<ExceptionHandler> exceptionHandlers;
            blocks.GetCode(out instructions, out exceptionHandlers);
            // reconstruct method
            DotNetUtils.RestoreBody(methodDef, instructions, exceptionHandlers);
            return false;
        } //..stripCallChecks

        public static bool hasCflow(dnlib.DotNet.MethodDef methods)
        {
            for (int i = 0; i < methods.Body.Instructions.Count; i++)
            {
                if (methods.Body.Instructions[i].OpCode == OpCodes.Switch)
                {
                    return true;
                }
            }
            return false;
        }
        public static void cleaner(ModuleDefMD module, IList<TypeDef> typesall, bool bExtra)
        {
            foreach (TypeDef types in typesall)
            {
                // subclasses yet again:
                if (types.NestedTypes.Count > 0)
                    cleaner(module, types.NestedTypes, bExtra);

                foreach (MethodDef methods in types.Methods)
                {
                    if (!methods.HasBody)
                        continue;
                    if (!bExtra)
                        stripCallChecks(methods);
                    if (hasCflow(methods))
                    {
                        if (Program.veryVerbose)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("Cleaning Control Flow for {0:X}\nThe case order is: ",
                                          methods.MDToken.ToInt32());
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        DeobfuscateCflow(methods);

                        if (Program.veryVerbose)
                            Console.WriteLine();
                    }
                }
            }
        }
    }
}
