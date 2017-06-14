using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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
            var regs = new List<LuaValue>();

            if (allowInclude)
            {
                throw new NotImplementedException();
            }

            var consts = chunk.RootFunction.Constants;
            for (var pc = 0; pc < chunk.RootFunction.Code.Length; pc++)
            {
                var instr = chunk.RootFunction.Code[pc];
                var opcode = (OpCode) InstructionMask.GetOpCode(instr);

                switch (opcode)
                {
                    case OpCode.Move:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        regs[a] = regs[b];
                        break;
                    }
                    case OpCode.LoadK:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var bx = InstructionMask.GetArgBx(instr);
                        regs[a] = consts[bx];
                        break;
                    }
                    case OpCode.LoadKx:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        instr = chunk.RootFunction.Code[++pc];
                        if (InstructionMask.GetOpCode(instr) != (byte) OpCode.ExtraArg)
                        {
                            throw new NotSupportedException("LOADKX must be followed by EXTRAARG");
                        }
                        var ax = InstructionMask.GetArgAx(instr);
                        regs[a] = consts[ax];
                        break;
                    }
                    case OpCode.LoadBool:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        regs[a] = b != 0;
                        if (c != 0)
                        {
                            pc++;
                        }
                        break;
                    }
                    case OpCode.LoadNil:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        for (; b >= 0; b--)
                        {
                            regs[a + b] = LuaValue.Nil;
                        }
                        break;
                    }
                    case OpCode.GetUpVal:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        regs[a] = GetUpVal(b);
                        break;
                    }
                    case OpCode.GetTabUp:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        var rc = GetRK(regs, consts, c);
                        regs[a] = GetUpVal(b)[rc];
                        break;
                    }
                    case OpCode.GetTable:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        var rc = GetRK(regs, consts, c);
                        regs[a] = regs[b][rc];
                        break;
                    }
                    case OpCode.SetTabUp:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);
                        GetUpVal(a)[rb] = rc;
                        break;
                    }
                    case OpCode.SetUpVal:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        SetUpVal(b, regs[a]);
                        break;
                    }
                    case OpCode.SetTable:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);
                        regs[a][rb] = rc;
                        break;
                    }
                    case OpCode.NewTable:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        var sizeB = b < 8 ? b : (b & 7) + 8 << (b >> 3) - 1;
                        var sizeC = c < 8 ? c : (c & 7) + 8 << (c >> 3) - 1;
                        regs[a] = new LuaValue(LuaValueType.Table, new LuaTable(sizeB, sizeC));
                        break;
                    }
                    case OpCode.Self:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        regs[a + 1] = regs[b];
                        regs[a] = regs[b][GetRK(regs, consts, c)];
                        break;
                    }
                    case OpCode.Add:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb + rc;
                        break;
                    }
                    case OpCode.Sub:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb - rc;
                        break;
                    }
                    case OpCode.Mul:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb * rc;
                        break;
                    }
                    case OpCode.Mod:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb % rc;
                        break;
                    }
                    case OpCode.Pow:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = LuaValue.Pow(rb, rc);
                        break;
                    }
                    case OpCode.Div:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb / rc;
                        break;
                    }
                    case OpCode.IDiv:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = LuaValue.IDiv(rb, rc);
                        break;
                    }
                    case OpCode.BAnd:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb & rc;
                        break;
                    }
                    case OpCode.BOr:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb | rc;
                        break;
                    }
                    case OpCode.BXor:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = rb ^ rc;
                        break;
                    }
                    case OpCode.Shl:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = LuaValue.Shl(rb, rc);
                        break;
                    }
                    case OpCode.Shr:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var rb = GetRK(regs, consts, b);
                        var rc = GetRK(regs, consts, c);

                        regs[a] = LuaValue.Shr(rb, rc);
                        break;
                    }
                    case OpCode.UnM:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);

                        var rb = GetRK(regs, consts, b);

                        regs[a] = -rb;
                        break;
                    }
                    case OpCode.BNot:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);

                        var rb = GetRK(regs, consts, b);

                        regs[a] = ~rb;
                        break;
                    }
                    case OpCode.Not:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);

                        var rb = GetRK(regs, consts, b);

                        regs[a] = !rb;
                        break;
                    }
                    case OpCode.Jmp:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var sbx = InstructionMask.GetArgSBx(instr);
                        pc += sbx;
                        if (a != 0)
                        {
                            CloseUpVals(regs, a - 1);
                        }
                        break;
                    }
                    case OpCode.Eq:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var x = GetRK(regs, consts, b) == GetRK(regs, consts, c);
                        if (x != (a != 0))
                        {
                            pc++;
                        }
                        break;
                    }
                    case OpCode.Lt:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var x = GetRK(regs, consts, b) < GetRK(regs, consts, c);
                        if (x != (a != 0))
                        {
                            pc++;
                        }
                        break;
                    }
                    case OpCode.Le:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        var x = GetRK(regs, consts, b) <= GetRK(regs, consts, c);
                        if (x != (a != 0))
                        {
                            pc++;
                        }
                        break;
                    }
                    case OpCode.Test:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var c = InstructionMask.GetArgC(instr);
                        if (regs[a].AsBoolean() != (c != 0))
                        {
                            pc++;
                        }
                        break;
                    }
                    case OpCode.TestSet:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        if (regs[b].AsBoolean() == (c != 0))
                        {
                            regs[a] = regs[b];
                        }
                        else
                        {
                            pc++;
                        }
                        break;
                    }
                    case OpCode.ForLoop:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var sbx = InstructionMask.GetArgSBx(instr);
                        var s = regs[a + 2];
                        regs[a] += s;
                        if (s < LuaValue.Zero ? regs[a] >= regs[a + 1] : regs[a] <= regs[a + 1])
                        {
                            pc += sbx;
                            regs[a + 3] = regs[a];
                        }
                        break;
                    }
                    case OpCode.ForPrep:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var sbx = InstructionMask.GetArgSBx(instr);
                        regs[a] -= regs[a + 2];
                        pc += sbx;
                        break;
                    }
                    default:
#if DEBUG
                        Console.WriteLine("unknown OP_CODE: " + opcode);
                        break;
#else
                        throw new NotSupportedException("unsupported OP_CODE");
#endif
                }
            }
        }

        private static void CloseUpVals(List<LuaValue> regs, int i)
        {
        }

        private static void SetUpVal(int index, LuaValue value)
        {
            throw new NotImplementedException();
        }

        private static LuaValue GetUpVal(int index)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LuaValue GetRK(IReadOnlyList<LuaValue> registers, IReadOnlyList<LuaValue> constants, short rk)
        {
            return (rk & InstructionMask.MaskK) != 0 ? constants[rk & ~InstructionMask.MaskK] : registers[rk];
        }

        private static LuaTable DefaultEnvironment()
        {
            return new LuaEnvironment();
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

            a.Files = args.SkipWhile(s => s.StartsWith("-")).ToArray();

            return a;
        }

        private enum OpCode : byte
        {
            Move,
            LoadK,
            LoadKx,
            LoadBool,
            LoadNil,
            GetUpVal,
            GetTabUp,
            GetTable,
            SetTabUp,
            SetUpVal,
            SetTable,
            NewTable,
            Self,
            Add,
            Sub,
            Mul,
            Mod,
            Pow,
            Div,
            IDiv,
            BAnd,
            BOr,
            BXor,
            Shl,
            Shr,
            UnM,
            BNot,
            Not,
            Len,
            Concat,
            Jmp,
            Eq,
            Lt,
            Le,
            Test,
            TestSet,
            Call,
            TailCall,
            Return,
            ForLoop,
            ForPrep,
            TForCall,
            TForLoop,
            SetList,
            Closure,
            VarArg,
            ExtraArg
        }

        private static class InstructionMask
        {
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

            private const uint MaskOpCode = ~(~(uint) 0 << SizeOpCode) << PosOpCode;
            private const uint MaskA = ~(~(uint) 0 << SizeA) << PosA;
            private const uint MaskB = ~(~(uint) 0 << SizeB) << PosB;
            private const uint MaskC = ~(~(uint) 0 << SizeC) << PosC;
            private const uint MaskAx = ~(~(uint) 0 << SizeAx) << PosAx;
            private const uint MaskBx = ~(~(uint) 0 << SizeBx) << PosBx;
            public const short MaskK = 1 << (SizeB - 1);

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
    }
}