using System;
using System.IO;
using System.Linq;

namespace LuaByteSharp.Lua
{
    public static class Interpreter
    {
        private struct Arguments
        {
            public bool Help;
            public bool StdIn;
            public bool AllowInclude;
            public string[] Files;
        }

        public static void Run(string[] args)
        {
            var pArgs = ParseArgs(args);
            if (pArgs.Help)
            {
                PrintHelp();
                return;
            }

            Stream[] input = null;
            try
            {
                input = pArgs.StdIn
                    ? new[] {Console.OpenStandardInput()}
                    : pArgs.Files
                        .Select(path => File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                        .ToArray<Stream>();

                var env = DefaultEnvironment();
                foreach (var stream in input)
                {
                    var chunk = LuaChunk.Load(stream);
                    ExecuteChunk(env, chunk, pArgs.AllowInclude);
                }
            }
            finally
            {
                if (input != null)
                {
                    foreach (var stream in input)
                    {
                        stream.Dispose();
                    }
                }
            }
        }

        private static void ExecuteChunk(LuaTable env, LuaChunk chunk, bool allowInclude)
        {
            if (allowInclude)
            {
                throw new NotImplementedException();
            }

            foreach (var instr in chunk.RootFunction.Code)
            {
                var opcode = InstructionMask.GetOpCode(instr);
                switch (opcode)
                {
                    default:
                        throw new NotSupportedException("unsupported OP_CODE");
                }
            }
        }

        private static LuaTable DefaultEnvironment()
        {
            return new LuaTable();
        }

        private static void PrintHelp()
        {
            const string help = "LuaByteSharp: A Lua Interpreter written in C#\n" +
                                "Version 0.0.1\n" +
                                "Copyright (c) 2017 Klemens Böswirth\n" +
                                "usage: lbs.exe [options] [files]\n" +
                                "\n" +
                                "options:\n" +
                                "\t-h, --help:\tprints this message and exits\n" +
                                "\t-s, --stdin:\tuses stdin instead of files\n" +
                                "\t-i, --include:\tallow use of the custom include(string) function\n" +
                                "\t\t\tthe include(string) function loads and immediately executes the given file";
            Console.WriteLine(help);
        }

        private static Arguments ParseArgs(string[] args)
        {
            var a = new Arguments();
            if (args.Contains("-h") || args.Contains("--help") || args.Contains("-?"))
            {
                a.Help = true;
            }

            if (args.Contains("-s") || args.Contains("--stdin"))
            {
                a.StdIn = true;
            }

            if (args.Contains("-i") || args.Contains("--include"))
            {
                a.AllowInclude = true;
            }

            a.Files = args.SkipWhile(s => !s.StartsWith("-")).ToArray();

            return a;
        }
    }

    internal static class InstructionMask
    {
        private const uint MaskOpCode = 0x0000003F;
        private const uint MaskA = 0x00003FC0;
        private const uint MaskB = 0xFF800000;
        private const uint MaskC = 0x007FC000;
        private const uint MaskAx = 0xFFFFFFC0;
        private const uint MaskBx = 0xFFFFC000;

        private const byte PosOpCode = 0;
        private const byte PosA = PosOpCode + SizeOpCode;
        private const byte PosB = PosC + SizeC;
        private const byte PosC = PosA + SizeA;
        private const byte PosAx = PosA;
        private const byte PosBx = PosC;

        private const byte SizeOpCode = 6;
        private const byte SizeA = 8;
        private const byte SizeB = 9;
        private const byte SizeC = 9;
        private const byte SizeAx = SizeA + SizeB + SizeC;
        private const byte SizeBx = SizeB + SizeC;

        private const int MaxSBx = 0x1FFFF;

        public static byte GetOpCode(uint instr)
        {
            return (byte) ((instr & MaskOpCode) >> PosOpCode);
        }

        public static byte GetArgA(uint instr)
        {
            return (byte) ((instr & MaskA) >> PosA);
        }

        public static short GetArgB(uint instr)
        {
            return (short) ((instr & MaskB) >> PosB);
        }

        public static short GetArgC(uint instr)
        {
            return (short) ((instr & MaskC) >> PosC);
        }

        public static int GetArgAx(uint instr)
        {
            return (int) ((instr & MaskAx) >> PosAx);
        }

        public static int GetArgBx(uint instr)
        {
            return (int) ((instr & MaskBx) >> PosBx);
        }

        public static int GetArgSBx(uint instr)
        {
            return (int) ((instr & MaskBx) >> PosBx) - MaxSBx;
        }
    }

    public class LuaTable
    {
    }
}