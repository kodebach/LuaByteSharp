using System.IO;

namespace LuaByteSharp.Lua
{
    public class LuaChunk
    {
        internal readonly LuaFunction RootFunction;
        internal byte GlobalUpValues;

        private LuaChunk(LuaFunction rootFunction)
        {
            RootFunction = rootFunction;
        }

        public static LuaChunk Load(string path)
        {
            using (var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                if (!LuaHeader.CheckHeader(reader))
                {
                    return null;
                }

                var globalUpValues = reader.ReadByte();

                var rootFunction = LuaFunction.Load(reader);
                return new LuaChunk(rootFunction)
                {
                    GlobalUpValues = globalUpValues
                };
            }
        }
    }
}