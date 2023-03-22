using FSUIPC;
using Serilog;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PilotsDeck_FNX2PLD
{
    public static class IPCManager
    {
        public static Offset airOffset = new(Program.groupName, 0x3C00, 256);
        public static Offset readytofly = new Offset<byte>(Program.groupName, 0x026D);
        public static readonly int waitDuration = 30000;

        public static MobiSimConnect SimConnect { get; set; } = null;

        public static bool WaitForSimulator(CancellationToken cancellationToken)
        {
            bool simRunning = IsSimRunning();
            if (!simRunning && Program.waitForConnect)
            {
                do
                {
                    Log.Logger.Information($"WaitForSimulator: Simulator not started - waiting {waitDuration/1000}s for Sim");
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !cancellationToken.IsCancellationRequested);

                return true;
            }
            else if (simRunning)
            {
                Log.Logger.Information($"WaitForSimulator: Simulator started");
                return true;
            }
            else
            {
                Log.Logger.Error($"WaitForSimulator: Simulator not started - aborting");
                return false;
            }
        }

        public static bool IsProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null;
        }

        public static bool IsSimRunning()
        {
            return IsProcessRunning("FlightSimulator");
        }

        public static bool WaitForConnection(CancellationToken cancellationToken)
        {
            bool isConnected = OpenSafeFSUIPC();
            if (!isConnected && Program.waitForConnect)
            {
                do
                {
                    Log.Logger.Information($"WaitForConnection: FSUIPC not connected - waiting {waitDuration / 1000}s for Retry");
                    Thread.Sleep(waitDuration);
                    isConnected = OpenSafeFSUIPC();
                }
                while (!isConnected && !cancellationToken.IsCancellationRequested);

                return isConnected && IsSimRunning();
            }
            else if (isConnected)
            {
                Log.Logger.Information($"WaitForConnection: FSUIPC connected");
                return true;
            }
            else
            {
                Log.Logger.Error($"WaitForConnection: FSUIPC not connected - aborting");
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
            catch
            {
                Log.Logger.Error($"OpenSafeFSUIPC: Exception while connecting to FSUIPC");
            }

            return FSUIPCConnection.IsOpen;
        }

        public static bool WaitForFenixAircraft(CancellationToken cancellationToken)
        {
            if (!airOffset.IsConnected)
                airOffset.Reconnect();

            bool processResult = FSUIPCProcess();
            while (IsSimRunning() && OpenSafeFSUIPC() && !processResult && !cancellationToken.IsCancellationRequested)
            {
                Log.Logger.Information($"WaitForFenixAircraft: FSUIPC not ready for Process - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                processResult = FSUIPCProcess();
            }

            if (!processResult)
            {
                Log.Logger.Error($"WaitForFenixAircraft: FSUIPC Connection or Simulator not available - aborting");
                return false;
            }

            while (IsSimRunning() && OpenSafeFSUIPC() && !IsAircraftFenix() && !cancellationToken.IsCancellationRequested)
            {
                Log.Logger.Information($"WaitForFenixAircraft: Current Aircraft is not the Fenix A320 - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                FSUIPCProcess();
            }

            if (!IsAircraftFenix())
            {
                Log.Logger.Error($"WaitForFenixAircraft: FSUIPC Connection or Simulator not available - aborting");
                return false;
            }
            else
                return true;
        }

        public static bool IsAircraftFenix()
        {
            return GetAircraftString().ToLower().Contains("fnx320");
        }

        public static string GetAircraftString()
        {
            try
            {
                string airString = airOffset.GetValue<string>();

                if (!string.IsNullOrEmpty(airString))
                {
                    return airString;
                }
                else
                    return "";
            }
            catch
            {
                return "";
            }
        }

        public static bool FSUIPCProcess()
        {
            bool result = false;

            if (FSUIPCConnection.IsOpen)
            {
                try
                {
                    FSUIPCConnection.Process(Program.groupName);
                    result = true;
                }
                catch
                {
                    Log.Logger.Error($"Process: Exception during Process() Call");
                }
            }
            else
            {
                Log.Logger.Error($"Process: FSUIPC Connection closed");
            }

            return result;
        }

        public static bool WaitForFenixBinary(CancellationToken cancellationToken)
        {
            if (!IsSimRunning())
                return false;

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();

            bool isRunning = IsProcessRunning(Program.FenixExecutable);
            if (!isRunning || !SimConnect.IsConnected)
            {
                do
                {
                    Log.Logger.Information($"WaitForFenixBinary: {Program.FenixExecutable} is not running - waiting {waitDuration / 2 / 1000}s for Retry");
                    Thread.Sleep(waitDuration / 2);
                    if (!mobiRequested)
                        mobiRequested = SimConnect.Connect();
                    isRunning = IsProcessRunning(Program.FenixExecutable);
                }
                while ((!isRunning || !SimConnect.IsConnected) && IsAircraftFenix() && IsSimRunning() && !cancellationToken.IsCancellationRequested);

                return isRunning && IsSimRunning();
            }
            else
            {
                Log.Logger.Information($"WaitForFenixBinary: {Program.FenixExecutable} is running");
                return true;
            }
        }

        public static bool WaitForSessionReady(CancellationToken cancellationToken)
        {
            if (!readytofly.IsConnected)
                readytofly.Reconnect();

            int waitDuration = 5000;
            bool isReady = IsCamReady();
            while (IsSimRunning() && OpenSafeFSUIPC() && FSUIPCProcess() && !isReady && !cancellationToken.IsCancellationRequested)
            {
                Log.Logger.Information($"WaitForSessionReady: Session not ready - waiting {waitDuration / 1000}s for Retry");
                Log.Logger.Information($"WaitForSessionReady: IsCamReady [{isReady}] readytofly [{readytofly.GetValue<byte>()}]");
                Thread.Sleep(waitDuration);
                FSUIPCProcess();
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                Log.Logger.Error($"WaitForSessionReady: FSUIPC Connection or Simulator not available - aborting");
                return false;
            }

            return true;
        }

        public static bool IsCamReady()
        {
            byte value = readytofly.GetValue<byte>();

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

                if (FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Close();
            }
            catch { }
            if (!FSUIPCConnection.IsOpen)
                Log.Logger.Information($"IPCManager: FSUIPC Connection closed");
            else
                Log.Logger.Warning($"IPCManager: FSUIPC still open!");
        }

        public static float ReadLVar(string name)
        {
            return SimConnect.ReadLvar(name);
        }


    }
}
