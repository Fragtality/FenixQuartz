using FSUIPC;
using Serilog;
using System.Diagnostics;

namespace PilotsDeck_FNX2PLD
{
    public static class IPCManager
    {
        public static string currentAirString = "";
        public static Offset airOffset = new Offset(Program.groupName, 0x3C00, 256);

        public static void WaitForConnection()
        {
            bool first = true;
            do
            {
                try
                {
                    if (!first)
                    {
                        Log.Logger.Information($"IPCManager: Waiting 15s before next Connection Attempt...");
                        Thread.Sleep(15000);
                    }
                    first = false;
                    FSUIPCConnection.Open();
                }
                catch { }
            }
            while (!FSUIPCConnection.IsOpen);

            Offset readytofly = new Offset<byte>(Program.groupName, 0x3364);
            Offset menudialog = new Offset<byte>(Program.groupName, 0x3365);
            FSUIPCConnection.Process(Program.groupName);

            first = true;
            while (readytofly.GetValue<byte>() != 0 || menudialog.GetValue<byte>() != 0)
            {
                try
                {
                    if (!first)
                    {
                        Log.Logger.Information($"IPCManager: Waiting 15s before next Connection Attempt...");
                        Thread.Sleep(15000);
                    }
                    first = false;
                    FSUIPCConnection.Process(Program.groupName);
                }
                catch { }
            }

            Log.Logger.Information($"IPCManager: MSFS2020 / FSUIPC seem to be running!");
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
                Log.Logger.Error(ex.Message);
            }

            return FSUIPCConnection.IsOpen;
        }

        public static double ReadLVar(string name)
        {
            double result = 0;

            if (MSFSVariableServices.LVars.Exists(name))
                result = MSFSVariableServices.LVars[name].Value;

            return result;
        }

        public static bool IsSimOpen()
        {
            Process? msfs = Process.GetProcessesByName("FlightSimulator").FirstOrDefault(); 
            return msfs != null;
        }

        public static bool IsAircraftFenix()
        {
            return currentAirString.Contains("fnx320");
        }

        public static bool RefreshCurrentAircraft()
        {
            Log.Logger.Information("IPCManager: Read Current Aircraft");

            string airString = "none";
            if (OpenSafeFSUIPC())
            {
                try
                {
                    FSUIPCConnection.Process(Program.groupName);
                }
                catch
                {
                    Log.Logger.Error("IPCManager: Exception during Process");
                    return false;
                }
                airString = airOffset.GetValue<string>();

                if (airString != null && airString.Length > 0)
                {
                    if (!airString.Contains("fnx320") && !Program.ignoreCurrentAC)
                    {
                        Log.Logger.Warning("IPCManager: Current Aircraft is not a Fenix 320!");
                        return false;
                    }
                    currentAirString = airString;
                }
                else
                {
                    Log.Logger.Error("IPCManager: Could not read AIR File from 0x3C00!");
                    return false;
                }
            }
            else
            {
                Log.Logger.Error("IPCManager: FSUIPC Connection failed!");
                return false;
            }

            Log.Logger.Information($"IPCManager: FSUIPC connected, Aircraft loaded: {airString}");
            return true;
        }
    }
}
