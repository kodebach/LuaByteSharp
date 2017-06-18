using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LuaByteSharp.Lua;

namespace LuaByteSharp
{
    public class Program
    {
#if DEBUG

        public static void Main(string[] args)
        {
            foreach (var file in Directory.EnumerateFiles("tests", "*.luac", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".luac")))
            {
                Console.WriteLine($"=== running {file} ===");
                if (Debugger.IsAttached)
                {
                    Interpreter.Run(new[] {file});
                }
                else
                {
                    try
                    {
                        Interpreter.Run(new[] { file });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                Console.WriteLine("=== done ===");
            }

            Console.WriteLine("=== all done ===");
            Console.ReadLine();
        }

#else
        public static void Main(string[] args)
        {
            try
            {
                Interpreter.Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }
#endif
    }
}