using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FenixQuartz
{
    public struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public uint AllocationProtect;
        public uint __alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint __alignment2;
    }

    public struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        public ushort reserved;
        public uint pageSize;
        public ulong minimumApplicationAddress;
        public ulong maximumApplicationAddress;
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    public partial class MemoryScanner
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial int OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ReadProcessMemory(int hProcess, ulong lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [LibraryImport("kernel32.dll")]
        private static partial void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial int VirtualQueryEx(int hProcess, ulong lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        
        public static readonly int PROCESS_QUERY_INFORMATION = 0x0400;
        public static readonly int PROCESS_VM_READ = 0x0010;
        public static readonly int ChunkSize = 384 * 384;

        private int procHandle = 0;
        private Process process;
        private SYSTEM_INFO sysInfo;
        private readonly Dictionary<string, List<MemoryPattern>> uniquePatterns = new();

        public MemoryScanner(Process proc)
        {
            sysInfo = new();
            GetSystemInfo(out sysInfo);

            procHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, proc.Id);
            Logger.Log(LogLevel.Information, "MemoryScanner:MemoryScanner", $"Fenix procHandle is {procHandle}");
            process = proc;
        }

        public bool IsInitialized()
        {
            return procHandle != 0;
        }

        public bool FenixIsRunning()
        {
            return process != null && !process.HasExited;
        }

        private void IntializePatterns(List<MemoryPattern> patterns)
        {
            uniquePatterns.Clear();
            List<MemoryPattern> list;
            foreach (var pattern in patterns)
            {
                if (uniquePatterns.TryGetValue(pattern.Pattern, out List<MemoryPattern> value))
                {
                    list = value;
                    if (list != null)
                    {
                        list.Add(pattern);
                    }
                    else
                    {
                        list = new()
                        {
                            pattern
                        };
                        uniquePatterns[pattern.Pattern] = list;
                    }
                }
                else
                {
                    list = new()
                    {
                        pattern
                    };
                    uniquePatterns.Add(pattern.Pattern, list);
                }
            }
        }

        public void SearchPatterns(List<MemoryPattern> patterns)
        {
            Stopwatch watch = new();
            watch.Start();

            MEMORY_BASIC_INFORMATION64 memInfo = new();
            ulong addrBase;
            ulong addrMax = sysInfo.maximumApplicationAddress;
            patterns.ForEach(p => p.Matches = 0);
            IntializePatterns(patterns);

            addrBase = sysInfo.minimumApplicationAddress;

            while (addrBase < addrMax && VirtualQueryEx(procHandle, addrBase, out memInfo, 48) != 0 && patterns.Any(p => p.Location == 0))
            {
                if (memInfo.Protect == 0x04 && memInfo.State == 0x00001000)
                {
                    SearchRegion(memInfo.BaseAddress, memInfo.RegionSize);
                } 
                addrBase += memInfo.RegionSize;
            }

            watch.Stop();
            Logger.Log(LogLevel.Information, "MemoryScanner:SearchPatterns", string.Format("Pattern Search took {0}s", watch.Elapsed.TotalSeconds));

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void SearchRegion(ulong addrBase, ulong regionSize)
        {
            ulong addrEnd = addrBase + regionSize;
            byte[] memBuff = new byte[ChunkSize];
            int bytesRead = 0;
            int result;

            do
            {
                if (!ReadProcessMemory(procHandle, addrBase, memBuff, ChunkSize, ref bytesRead) || bytesRead < 512)
                {
                    addrBase += (ulong)ChunkSize;
                    continue;
                }

                foreach (var patternList in uniquePatterns.Values)
                {
                    if (patternList.All(p => p.Location != 0))
                        continue;

                    result = -1;

                    if (!patternList.First().HasWildCards)
                        result = BoyerMoore.IndexOf(memBuff, patternList.First().BytePattern);
                    else
                    {
                        var resultList = BoyerMooreHorspool.SearchPattern(memBuff, patternList.First().PatternTuple, patternList.First().Pattern.Length);
                        result = resultList.Count > 0 ? resultList[0] : -1;
                    }

                    if (result != -1)
                    {
                        foreach (var pattern in patternList)
                        {
                            if (pattern.Location == 0)
                            {
                                pattern.Matches++;
                                if (pattern.Matches == pattern.MatchNumber)
                                    pattern.Location = addrBase + (ulong)result;
                            }
                        }
                    }
                }

                addrBase += (ulong)ChunkSize;
            }
            while (addrBase < addrEnd);

            memBuff = null;
        }

        public static ulong CalculateLocation(ulong baseAddr, long offset)
        {
            if (offset < 0)
                baseAddr -= (ulong)(-offset);
            else
                baseAddr += (ulong)offset;

            return baseAddr;
        }

        public bool UpdateBuffers(Dictionary<string, MemoryValue> memValues)
        {
            int bytesRead = 0;
            byte[] memBuff;

            if (!FenixIsRunning())
                return false;

            foreach (var memValue in memValues.Values)
            {
                if (memValue.Pattern.Location == 0)
                    continue;

                memBuff = new byte[memValue.Size];
                if (ReadProcessMemory(procHandle, CalculateLocation(memValue.Pattern.Location, memValue.PatternOffset), memBuff, memValue.Size, ref bytesRead))
                    memValue.UpdateBuffer(memBuff);
            }

            return true;
        }
    }
}
