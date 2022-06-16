using Serilog;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PilotsDeck_FNX2PLD
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

    public class MemoryScanner
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(int hProcess, ulong lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(int hProcess, ulong lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        
        public static readonly int PROCESS_QUERY_INFORMATION = 0x0400;
        public static readonly int PROCESS_VM_READ = 0x0010;
        public static readonly int ChunkSize = 1024 * 1024;

        private int procHandle = 0;
        private SYSTEM_INFO sysInfo;

        public MemoryScanner(Process proc)
        {
            sysInfo = new();
            GetSystemInfo(out sysInfo);

            procHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, proc.Id);
        }

        public bool IsInitialized()
        {
            return procHandle != 0;
        }

        public void SearchPatterns(List<MemoryPattern> patterns)
        {
            Stopwatch watch = new();
            watch.Start();

            MEMORY_BASIC_INFORMATION64 memInfo = new();
            ulong addrBase;
            ulong addrMax = sysInfo.maximumApplicationAddress;
            int matches;

            foreach(var pattern in patterns)
            {
                addrBase = sysInfo.minimumApplicationAddress;
                matches = 0;

                while (addrBase < addrMax && pattern.Location == 0 && VirtualQueryEx(procHandle, addrBase, out memInfo, 48) != 0)
                {
                    if (memInfo.Protect == 0x04 && memInfo.State == 0x00001000)
                    {
                        SearchRegion(pattern, ref matches, memInfo.BaseAddress, memInfo.RegionSize);
                    } 
                    addrBase += memInfo.RegionSize;
                }

            }

            watch.Stop();
            Log.Information(string.Format("MemoryScanner: Pattern Search took {0}s", watch.Elapsed.TotalSeconds));
        }

        private void SearchRegion(MemoryPattern pattern, ref int matches, ulong addrBase, ulong regionSize)
        {
            ulong addrEnd = addrBase + regionSize;
            byte[] memBuff = new byte[ChunkSize];
            int bytesRead = 0;
            int result;
            int lastResult = -1;
            bool readNewChunk = true;

            do
            {
                if (readNewChunk)
                {
                    if (!ReadProcessMemory(procHandle, addrBase, memBuff, ChunkSize, ref bytesRead) || bytesRead != ChunkSize)
                    {
                        addrBase += (ulong)ChunkSize;
                        continue;
                    }
                    else
                    {
                        readNewChunk = false;
                        lastResult = -1;
                    }
                }
                else if (lastResult != -1)
                {
                    if (lastResult + pattern.BytePattern.Length < ChunkSize)
                        Array.Fill<byte>(memBuff, 0, 0, lastResult + pattern.BytePattern.Length);
                    else
                    {
                        readNewChunk = true;
                        lastResult = -1;
                        addrBase += (ulong)ChunkSize;
                        continue;
                    }
                }

                result = BoyerMoore.IndexOf(memBuff, pattern.BytePattern);
                if (result != -1)
                {
                    matches++;
                    if (matches == pattern.MatchNumber)
                        pattern.Location = addrBase + (ulong)result;
                    else
                        lastResult = result;
                }
                else
                    lastResult = result;

                if (lastResult == -1 || lastResult >= ChunkSize - pattern.BytePattern.Length)
                {
                    addrBase += (ulong)ChunkSize;
                    readNewChunk = true;
                }
            }
            while (pattern.Location == 0 && addrBase < addrEnd);
        }

        public static ulong CalculateLocation(ulong baseAddr, long offset)
        {
            if (offset < 0)
                baseAddr -= (ulong)(-offset);
            else
                baseAddr += (ulong)offset;

            return baseAddr;
        }

        public void UpdateBuffers(Dictionary<string, MemoryPattern> patterns)
        {
            int bytesRead = 0;
            byte[] memBuff;

            foreach (var pattern in patterns)
            {
                if (pattern.Value.Location == 0)
                    continue;

                foreach (var offset in pattern.Value.MemoryOffsets.Values)
                {
                    memBuff = new byte[offset.Size];
                    if (ReadProcessMemory(procHandle, CalculateLocation(pattern.Value.Location, offset.AddressOffset), memBuff, offset.Size, ref bytesRead))
                        offset.UpdateBuffer(memBuff);
                }
            }
        }
    }
}
