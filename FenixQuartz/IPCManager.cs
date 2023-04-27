using FSUIPC;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FenixQuartz
{
    public static class IPCManager
    {
        public static readonly int waitDuration = 30000;

        public static MobiSimConnect SimConnect { get; set; } = null;

        public static bool WaitForSimulator()
        {
            bool simRunning = IsSimRunning();
            if (!simRunning && App.waitForConnect)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator not started - waiting {waitDuration / 1000}s for Sim");
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !App.CancellationRequested);

                return true;
            }
            else if (simRunning)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator started");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSimulator", $"Simulator not started - aborting");
                return false;
            }
        }

        public static bool IsProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null && proc.ProcessName == name;
        }

        public static bool IsSimRunning()
        {
            return IsProcessRunning("FlightSimulator");
        }

        public static bool WaitForConnection()
        {
            if (!IsSimRunning())
                return false;

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();
            
            bool isFsuipcConnected;
            if (!App.useLvars)
                isFsuipcConnected = OpenSafeFSUIPC();
            else
                isFsuipcConnected = true;

            int waitMS = waitDuration / 2;
            int countdown = 1000;
            if (!IsProcessRunning(App.FenixExecutable))
                countdown = waitMS;
            if ((!App.useLvars && !isFsuipcConnected) || !SimConnect.IsConnected)
            {
                do
                {
                    if (countdown == waitMS)
                        Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"Connection not established - waiting {waitMS / 1000}s for Retry");

                    countdown -= 1000;
                    Thread.Sleep(1000);

                    if (!IsSimRunning())
                        break;

                    if (countdown == 0)
                    {
                        if (!App.useLvars && !isFsuipcConnected)
                            isFsuipcConnected = OpenSafeFSUIPC();

                        if (!mobiRequested)
                            mobiRequested = SimConnect.Connect();

                        countdown = waitMS;
                    }
                }
                while ((!App.useLvars && !isFsuipcConnected) || !SimConnect.IsConnected);

                return isFsuipcConnected && SimConnect.IsConnected && IsSimRunning();
            }
            else if (isFsuipcConnected && SimConnect.IsConnected && IsSimRunning())
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"Connection established");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForConnection", $"Connection failed");
                return false;
            }
        }

        public static bool OpenSafeFSUIPC()
        {
            try
            {
                if (!FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Open();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "IPCManager:OpenSafeFSUIPC", $"Exception while connecting to FSUIPC ({ex.GetType()} {ex.Message})");
            }

            return FSUIPCConnection.IsOpen;
        }

        public static bool WaitForFenixBinary()
        {
            if (!IsSimRunning())
                return false;

            bool isRunning = IsProcessRunning(App.FenixExecutable);
            if (!isRunning)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForFenixBinary", $"{App.FenixExecutable} is not running - waiting {waitDuration / 2 / 1000}s for Retry");
                    Thread.Sleep(waitDuration / 2);

                    isRunning = IsProcessRunning(App.FenixExecutable);
                }
                while (!isRunning && IsSimRunning() && !App.CancellationRequested);

                return isRunning && IsSimRunning();
            }
            else
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForFenixBinary", $"{App.FenixExecutable} is running");
                return true;
            }
        }

        public static bool WaitForSessionReady()
        {
            int waitDuration = 5000;
            SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");
            Thread.Sleep(250);
            bool isReady = IsCamReady();
            while (IsSimRunning() && !isReady && !App.CancellationRequested)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSessionReady", $"Session not ready - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSessionReady", $"SimConnect or Simulator not available - aborting");
                return false;
            }

            return true;
        }

        public static bool IsCamReady()
        {
            float value = SimConnect.ReadSimVar("CAMERA STATE", "Enum");

            return value >= 2 && value <= 5;
        }

        public static void CloseSafe()
        {
            try
            {
                if (SimConnect != null)
                {
                    SimConnect.Disconnect();
                    SimConnect = null;
                }

                if (!App.useLvars && FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Close();
            }
            catch { }
        }
    }
}
