using System;
using LuaByteSharp.Lua;

namespace LuaByteSharp
{
    public class Program
    {
#if DEBUG
        public static void Main(string[] args)
        {
            Interpreter.Run(args);

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