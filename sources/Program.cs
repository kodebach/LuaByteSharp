using System;
using System.IO;
using LuaByteSharp.Lua;

namespace LuaByteSharp
{
    public class Program
    {
#if DEBUG

        public static void Main(string[] args)
        {
            foreach (var file in Directory.EnumerateFiles("tests"))
            {
                Console.WriteLine($"=== running {file} ===");
                Interpreter.Run(new[] {file});
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