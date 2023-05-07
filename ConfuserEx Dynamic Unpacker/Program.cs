
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker
{
class Program
{
    public static ModuleDefMD module;
    public static Assembly asm = null;

    public static bool veryVerbose = false;
    private static string path = null;
    private static string mode;

    static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        optionParser(args);
        Console.WriteLine("Yeah confuserex unpacker so what");

        if (path == null || mode == null)
        {
            Console.WriteLine("Check args make sure path and either -d or -s is included (Dynamic or static)");
            Console.ReadLine();
            return;
        }
        module = ModuleDefMD.Load(path);
        if (mode.ToLower() == "static")
        {
            staticRoute();
        }
        else if (mode.ToLower() == "dynamic")
        {
            dynamicRoute();
        }
        else
        {
            Console.Write("Yeah erm you might be a bit of an idiot follow the instructions");
            Console.ReadLine();
            return;
        }
        ModuleWriterOptions writerOptions = new ModuleWriterOptions(module);
        writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
        writerOptions.Logger = DummyLogger.NoThrowInstance;
        module.Write(GetDefaultNewFilename(path), writerOptions);
        Console.ReadLine();
    }
    static string GetDefaultNewFilename(string strFileName)
    {
        string newFilename =
            Path.GetFileNameWithoutExtension(strFileName) + "-cleaned" + Path.GetExtension(strFileName);
        return Path.Combine(Path.GetDirectoryName(strFileName), newFilename);
    }
    static void staticRoute()
    {
        antitamper();
        Protections.ControlFlowRun.cleaner(module, module.Types, false);
        Staticpacker();
        try
        {
            Console.WriteLine("[!] Cleaning Proxy Calls");
            int amountProxy = Protections.ReferenceProxy.ProxyFixer(module);
            Console.WriteLine("[!] Amount Of Proxy Calls Fixed: " + amountProxy);
            Protections.ControlFlowRun.cleaner(module, module.Types, true);
            Console.WriteLine("[!] Decrytping Strings");
            int strings = Protections.StaticStrings.Run(module);
            Console.WriteLine("[!] Amount Of Strings Decrypted: " + strings);
        }
        catch (Exception ex)
        {
            Console.WriteLine("error happened " + ex.ToString());
        }
    }
    static void dynamicRoute()
    {
        antitamper();
        Protections.ControlFlowRun.cleaner(module, module.Types, false);
        packer();
        try
        {
            Console.WriteLine("[!] Cleaning Proxy Calls");
            int amountProxy = Protections.ReferenceProxy.ProxyFixer(module);
            Console.WriteLine("[!] Amount Of Proxy Calls Fixed: " + amountProxy);
            Protections.ControlFlowRun.cleaner(module, module.Types, true);
            Console.WriteLine("[!] Decrytping Strings");
            // save modified file:
            ModuleWriterOptions writerOptions = new ModuleWriterOptions(module);
            writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            writerOptions.Logger = DummyLogger.NoThrowInstance;
            var szFileName = Path.GetTempFileName();
            module.Write(szFileName, writerOptions);
            // load assemblies:
            asm = /* Assembly.LoadFrom(szFileName);  -- can't unload file */
                Assembly.Load(File.ReadAllBytes(szFileName));
            int strings = Protections.Constants.constants(module, module.Types);
            Console.WriteLine("[!] Amount Of Strings Decrypted: " + strings);

            File.Delete(szFileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("error happened " + ex.ToString());
        }
    }
    static void optionParser(string[] str)
    {
        foreach (string arg in str)
        {
            switch (arg)
            {

            case "-vv":
                veryVerbose = true;

                break;
            case "-d":
                mode = "dynamic";
                break;
            case "-s":
                mode = "static";
                break;
            default:
                path = arg;
                break;
            }
        }
    }
    static void packer()
    {
        try
        {
            if (Protections.Packer.IsPacked(module))
            {
                Console.WriteLine("[!] Compressor Detected");
                try
                {
                    Protections.Packer.findLocal();
                    Console.WriteLine("[!] Compressor Removed Successfully");
                    Console.WriteLine("[!] Now Cleaning The koi Module");
                }
                catch
                {
                    Console.WriteLine("[!] Compressor Failed To Remove");
                }

                antitamper();
                module.EntryPoint = module.ResolveToken(Protections.StaticPacker.epToken) as MethodDef;
            }
        }
        catch
        {
            Console.WriteLine("An error in dynamic packer remover happened");
        }
    }
    static void Staticpacker()
    {
        try
        {
            if (Protections.Packer.IsPacked(module))
            {
                Console.WriteLine("[!] Compressor Detected");
                try
                {
                    Protections.StaticPacker.Run(module);
                    Console.WriteLine("[!] Compressor Removed Successfully");
                    Console.WriteLine("[!] Now Cleaning The koi Module");
                }
                catch
                {
                    Console.WriteLine("[!] Compressor Failed To Remove");
                }

                antitamper();
                module.EntryPoint = module.ResolveToken(Protections.StaticPacker.epToken) as MethodDef;
            }
        }
        catch
        {
            Console.WriteLine("An error in static packer remover happened");
        }
    }
    static void antitamper()
    {
        try
        {
            if (Protections.AntiTamper.IsTampered(module) == true)
            {
                Console.WriteLine("[!] Anti Tamper Detected");

                byte[] rawbytes = null;

                var htdgfd = (module).Metadata.PEImage.CreateReader(); // .CreateFullStream();

                rawbytes = htdgfd.ReadBytes((int)htdgfd.Length);
                try
                {
                    module = Protections.AntiTamper.UnAntiTamper(module, rawbytes);
                    Console.WriteLine("[!] Anti Tamper Removed Successfully");
                }
                catch
                {
                    Console.WriteLine("[!] Anti Tamper Failed To Remove");
                }
            }
        }
        catch
        {
            Console.WriteLine("An error in anti tamper remover happened");
        }
    }
}
}
