using System.IO;

namespace LuaByteSharp.Lua
{
    internal class LuaFunction
    {
        public LuaString SourceName;
        public uint LineDefined;
        public uint LastLineDefined;
        public byte ParameterCount;
        public bool IsVarArg;
        public byte MaxStackSize;
        public uint[] Code;
        public dynamic[] Constants;
        public LuaUpValue[] UpValues;

        public LuaFunction[] Prototypes;

        public uint[] Lines;
        public LuaString[] Locals;
        public LuaString[] UpValueNames;

        public static LuaFunction Load(BinaryReader reader)
        {
            var function = new LuaFunction
            {
                SourceName = LoadHelper.LoadString(reader),
                LineDefined = reader.ReadUInt32(),
                LastLineDefined = reader.ReadUInt32(),
                ParameterCount = reader.ReadByte(),
                IsVarArg = reader.ReadBoolean(),
                MaxStackSize = reader.ReadByte(),
                Code = LoadHelper.LoadCode(reader),
                Constants = LoadHelper.LoadConstants(reader),
                UpValues = LoadHelper.LoadUpValues(reader)
            };
            var sizeP = reader.ReadUInt32();
            function.Prototypes = new LuaFunction[sizeP];
            for (var i = 0; i < sizeP; i++)
            {
                function.Prototypes[i] = Load(reader);
            }

            var sizeLines = reader.ReadUInt32();
            function.Lines = new uint[sizeLines];
            for (var i = 0; i < sizeLines; i++)
            {
                function.Lines[i] = reader.ReadUInt32();
            }

            var sizeLocals = reader.ReadUInt32();
            function.Locals = new LuaString[sizeLocals];
            for (var i = 0; i < sizeLocals; i++)
            {
                function.Locals[i] = LoadHelper.LoadString(reader);
            }

            var sizeUpValueNames = reader.ReadUInt32();
            function.UpValueNames = new LuaString[sizeUpValueNames];
            for (var i = 0; i < sizeUpValueNames; i++)
            {
                function.UpValueNames[i] = LoadHelper.LoadString(reader);
            }

            return function;
        }
    }
}