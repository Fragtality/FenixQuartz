using Serilog;
using System.Text;

namespace PilotsDeck_FNX2PLD
{
    public class MemoryValue : IDisposable
    {
        public string ID { get; set; } = "";
        public MemoryPattern Pattern { get; set; }
        public long PatternOffset { get; set; }    
        public int Size { get; set; }
        public string TypeName { get; set; } = "int";
        public bool CastInteger { get; set; } = false;

        private byte[]? valueBuffer = null;

        public MemoryValue(string id, MemoryPattern pattern, long patternOffset, int size, string typeName, bool castInteger = false)
        {
            ID = id;
            Pattern = pattern;
            PatternOffset = patternOffset;
            Size = size;
            TypeName = typeName;
            CastInteger = castInteger;
            valueBuffer = new byte[size];
        }

        public void UpdateBuffer(byte[] memBuffer)
        {
            if (valueBuffer != null)
                Array.Copy(memBuffer, valueBuffer, memBuffer.Length);
            else
                Log.Logger.Error($"MemoryOffset: Error in UpdateBuffer() - valueBuffer is null");
        }

        public bool IsIntegerZero()
        {
            if (valueBuffer == null)
                return true;
            else if (BitConverter.ToInt32(valueBuffer, 0) == 0)
                return true;
            else return false;
        }

        public bool IsTinyValue()
        {
            if (valueBuffer == null)
                return false;

            if (TypeName == "double")
            {
                double dbl = BitConverter.ToDouble(valueBuffer, 0);
                if (dbl > -0.001 && dbl < 0.001 && dbl != 0.00)
                    return true;
                else
                    return false;
            }
            if (TypeName == "float")
            {
                double flt = BitConverter.ToSingle(valueBuffer, 0);
                if (flt > -0.001f && flt < 0.001f && flt != 0.00f)
                    return true;
                else
                    return false;
            }

            return false;
        }

        public virtual dynamic? GetValue()
        {
            try
            {
                if (valueBuffer == null)
                    return null;

                if (TypeName == "float" && !CastInteger)
                    return BitConverter.ToSingle(valueBuffer, 0);
                else if (TypeName == "double" && !CastInteger)
                    return BitConverter.ToDouble(valueBuffer, 0);
                else if (TypeName == "bool" || TypeName == "int" && Size == 1)
                    return BitConverter.ToBoolean(valueBuffer, 0);
                else if (TypeName == "int")
                    if (Size == 4)
                        return BitConverter.ToInt32(valueBuffer, 0);
                    else //if (Size == 2)
                        return BitConverter.ToInt16(valueBuffer, 0);
                else if (TypeName == "long")
                    return BitConverter.ToInt64(valueBuffer, 0);
                else // == string
                    return Encoding.ASCII.GetString(valueBuffer).Replace("\0", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MemoryOffset: Exception while converting Byte Value for: {ex.Source} - {ex.Message}");
            }

            return null;
        }

        public void Dispose()
        {
            valueBuffer = null;
        }
    }
}
