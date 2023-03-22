using System.Collections.Generic;

namespace FenixQuartz
{
    public class MemoryPattern
    {
        public byte[] BytePattern { get; set; }
        public ulong Location { get; set; } = 0;
        public int MatchNumber { get; set; } = 1;

        public MemoryPattern(string pattern)
        {
            BytePattern = ConvertPattern(pattern);
        }

        public MemoryPattern(string pattern, int match)
        {
            BytePattern = ConvertPattern(pattern);
            MatchNumber = match;
        }

        public static byte[] ConvertPattern(string patternStr)
        {
            List<byte> pattern = new();
            string[] bytesStr = patternStr.Split(' ');
            foreach (var hexStr in bytesStr)
            {
                pattern.Add(byte.Parse(hexStr, System.Globalization.NumberStyles.HexNumber));
            }
            return pattern.ToArray();
        }
    }
}
