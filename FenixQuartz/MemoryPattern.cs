using System;
using System.Collections.Generic;
using System.Linq;

namespace FenixQuartz
{
    //public class MemoryPattern
    //{
    //    public byte[] BytePattern { get; set; }
    //    public ulong Location { get; set; } = 0;
    //    public int MatchNumber { get; set; } = 1;

    //    public MemoryPattern(string pattern)
    //    {
    //        BytePattern = ConvertPattern(pattern);
    //    }

    //    public MemoryPattern(string pattern, int match)
    //    {
    //        BytePattern = ConvertPattern(pattern);
    //        MatchNumber = match;
    //    }

    //    public static byte[] ConvertPattern(string patternStr)
    //    {
    //        List<byte> pattern = new();
    //        string[] bytesStr = patternStr.Split(' ');
    //        foreach (var hexStr in bytesStr)
    //        {
    //            pattern.Add(byte.Parse(hexStr, System.Globalization.NumberStyles.HexNumber));
    //        }
    //        return pattern.ToArray();
    //    }
    //}
    public class MemoryPattern
    {
        public byte[] BytePattern { get; set; }
        public bool HasWildCards { get; set; } = false;
        public string Pattern { get; set; }
        public ulong Location { get; set; } = 0;
        public int MatchNumber { get; set; } = 1;
        public int Matches { get; set; } = 0;
        public (byte, bool)[] PatternTuple { get; set; }

        public MemoryPattern(string pattern)
        {
            Pattern = pattern;
            ConvertPattern(pattern);
        }

        public MemoryPattern(string pattern, int match) : this(pattern)
        {
            MatchNumber = match;
        }

        protected void ConvertPattern(string pattern)
        {
            PatternTuple = pattern.Split(' ')
                .Select(hex => hex.Contains('?')
                    ? (byte.MinValue, false)
                    : (Convert.ToByte(hex, 16), true))
                .ToArray();

            List<byte> patternList = new();
            string[] bytesStr = pattern.Split(' ');
            foreach (var hexStr in bytesStr)
            {
                if (hexStr != "??")
                    patternList.Add(byte.Parse(hexStr, System.Globalization.NumberStyles.HexNumber));
                else
                    patternList.Add(0xFF);
            }
            BytePattern = patternList.ToArray();

            HasWildCards = pattern.Contains("??");
        }
    }
}
