using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private const int FIELDS_PER_FLUSH = 50;

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

            var retvals = ExecuteClosure(new LuaClosure
            {
                Function = chunk.RootFunction,
                UpValues = {env}
            });
#if DEBUG
            Console.WriteLine(
                $"Function returned: {retvals.Aggregate("{", (current, value) => $"{current} {value},", s => s.TrimEnd(',') + "}")}"
            );
#endif
        }

        private static LuaValue[] ExecuteClosure(LuaClosure closure)
        {
            var allRegs = new LuaValue[closure.Function.MaxStackSize];
            var callStack = new Stack<LuaStackFrame>();
            var regs = new ArraySlice<LuaValue>(allRegs);
            var pc = 0;

            newframe:
            var function = closure.Function;
            var consts = function.Constants;
            var upDescs = function.UpValues;
            var protos = function.Prototypes;
            var upvals = closure.UpValues;

            for (; pc < function.Code.Length; pc++)
            {
                var instr = function.Code[pc];
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
                        instr = function.Code[++pc];
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
                        regs[a] = upvals[upDescs[b].Index];
                        break;
                    }
                    case OpCode.GetTabUp:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        var rc = GetRK(regs, consts, c);
                        regs[a] = upvals[upDescs[b].Index][rc];
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
                        upvals[upDescs[a].Index][rb] = rc;
                        break;
                    }
                    case OpCode.SetUpVal:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        upvals[upDescs[b].Index] = regs[a];
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
                        regs[a] = new LuaTable(sizeB, sizeC);
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
                    case OpCode.Len:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);

                        regs[a] = regs[b].Length;
                        break;
                    }
                    case OpCode.Concat:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);
                        regs[a] = LuaValue.Concat(regs.MoveAndResize(b, c - b + 1));
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
                    case OpCode.Call:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);
                        var c = InstructionMask.GetArgC(instr);

                        if (regs[a].Type == LuaValueType.Closure)
                        {
                            callStack.Push(new LuaStackFrame
                            {
                                Basep = regs.Offset,
                                Size = regs.Count,
                                RetReg = a,
                                RetCount = c,
                                Closure = closure,
                                SavedPC = pc
                            });
                            closure = (LuaClosure) regs[a].RawValue;
                            var parCount = closure.Function.ParameterCount; // number of fixed arguments accepted
                            var isVarArg = closure.Function.IsVarArg; // accepts vararg arguments?
                            var argCount = b == 0 ? regs.Count - (a + 1) : b - 1; // number arguments supplied

                            if (isVarArg)
                            {
                                
                            }
                            else
                            {
                                regs = regs.MoveAndResize(a + 1, parCount, true);
                                // fill up with nils (in new regs slice)
                                for (var i = argCount; i < parCount; i++)
                                {
                                    regs[i] = LuaValue.Nil;
                                }
                            }
                        }

                        if (regs[a].Type == LuaValueType.ExternalFunction)
                        {
                            var func = regs[a].RawValue as Func<LuaValue[], LuaValue[]>;

                            var argCount = b == 0 ? regs.Count - (a + 1) : b - 1;
                            var args = regs.MoveAndResize(a + 1, argCount);
                            var retvals = func.Invoke(args.ToArray());
                            if (retvals != null)
                            {
                                regs.SetAll(retvals, 0, a + 3, c, true);
                            }
                            break;
                        }

                        if (regs[a].Type == LuaValueType.ExternalAction)
                        {
                            var action = regs[a].RawValue as Action<LuaValue[]>;
                            var argCount = b == 0 ? regs.Count - (a + 1) : b - 1;
                            var args = regs.MoveAndResize(a + 1, argCount);
                            action.Invoke(args.ToArray());
                            break;
                        }

                        throw new NotSupportedException("cannot CALL that (metamethods)" + regs[a]);
                    }
                    case OpCode.Return:
                    {
                        CloseUpVals(regs, 0);
                        var a = InstructionMask.GetArgA(instr);
                        var b = InstructionMask.GetArgB(instr);

                        var retCount = b == 0 ? regs.Count - a : b - 1;

                        if (callStack.Count <= 0)
                        {
                            // external call
                            return regs.MoveAndResize(a, retCount).ToArray();
                        }

                        var parentFrame = callStack.Pop();
                        var parentRegs = new ArraySlice<LuaValue>(regs.Array, parentFrame.Basep, parentFrame.Size);
                        if (parentFrame.RetCount == 0)
                        {
                            parentRegs.SetAll(regs, a, parentFrame.RetReg, retCount, true);
                        }
                        else
                        {
                            parentRegs.SetAll(regs, a, parentFrame.RetReg, parentFrame.RetCount - 1, true);
                        }

                        regs = parentRegs;
                        closure = parentFrame.Closure;
                        pc = ++parentFrame.SavedPC;
                        goto newframe;
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
                    case OpCode.TForCall:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var c = InstructionMask.GetArgC(instr);

                        if (regs[a].Type == LuaValueType.Closure)
                        {
                            callStack.Push(new LuaStackFrame
                            {
                                Basep = regs.Offset,
                                Size = regs.Count,
                                RetReg = a,
                                RetCount = c,
                                Closure = closure,
                            });
                            regs = regs.MoveAndResize(a + 1, 2);
                            closure = (LuaClosure) regs[a].RawValue;
                            goto newframe;
                        }

                        if (regs[a].Type == LuaValueType.ExternalFunction)
                        {
                            var func = regs[a].RawValue as Func<LuaValue[], LuaValue[]>;

                            var args = regs.MoveAndResize(a + 1, 2);
                            var retvals = func.Invoke(args.ToArray());
                            if (retvals != null)
                            {
                                regs.SetAll(retvals, 0, a + 3, c, true);
                            }
                            break;
                        }

                        throw new ArgumentException("cannot TFORCALL that " + regs[a]);
                    }
                    case OpCode.TForLoop:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var sbx = InstructionMask.GetArgSBx(instr);
                        if (!regs[a].IsNil)
                        {
                            regs[a] = regs[a + 1];
                            pc += sbx;
                        }
                        break;
                    }
                    case OpCode.SetList:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        int b = InstructionMask.GetArgB(instr);
                        int c = InstructionMask.GetArgC(instr);
                        if (c == 0)
                        {
                            instr = function.Code[++pc];
                            if (InstructionMask.GetOpCode(instr) != (byte) OpCode.ExtraArg)
                            {
                                throw new NotSupportedException("SETLIST with c == 0 has to be followed by EXTRAARG");
                            }
                            c = InstructionMask.GetArgAx(instr);
                        }
                        if (b == 0)
                        {
                            b = regs.Count - a - 1;
                        }
                        for (var i = 1; i <= b; i++)
                        {
                            regs[a][(c - 1) * FIELDS_PER_FLUSH + i] = regs[a + i];
                        }
                        break;
                    }
                    case OpCode.Closure:
                    {
                        var a = InstructionMask.GetArgA(instr);
                        var bx = InstructionMask.GetArgBx(instr);
                        regs[a] = CreateClosure(protos[bx], regs, upvals);
                        break;
                    }
                    case OpCode.VarArg:
                    {
                        break;
                    }
                    case OpCode.ExtraArg:
                        break;
                    default:
#if DEBUG
                        Console.WriteLine("unknown OP_CODE: " + opcode);
                        break;
#else
                        throw new NotSupportedException("unsupported OP_CODE");
#endif
                }
            }

            throw new NotSupportedException("function did not return");
        }

        private struct LuaStackFrame
        {
            public LuaClosure Closure;
            public int Basep;
            public int Size;
            public int RetReg;
            public int RetCount;
            public int SavedPC;
        }

        private static LuaValue CreateClosure(LuaFunction function, IList<LuaValue> regs, IList<LuaValue> upvals)
        {
            var closure = new LuaClosure {Function = function, UpValues = upvals};
            var len = function.UpValues.Length;
            for (var i = 0; i < len; i++)
            {
                if (function.UpValues[i].InStack)
                {
                    closure.UpValues[len - i - 1] = regs[function.UpValues[i].Index];
                }
                else
                {
                    closure.UpValues[len - i - 1] = upvals[function.UpValues[i].Index];
                }
            }
            return new LuaValue(LuaValueType.Closure, closure);
        }

        private static void CloseUpVals(IList<LuaValue> regs, int i)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LuaValue GetRK(IList<LuaValue> registers, IList<LuaValue> constants, short rk)
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

    internal struct LuaClosure
    {
        public LuaFunction Function;
        public IList<LuaValue> UpValues;
    }
}