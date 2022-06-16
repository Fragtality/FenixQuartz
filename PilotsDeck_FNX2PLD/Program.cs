using FSUIPC;
using Serilog;
using System.Configuration;
using System.Diagnostics;

namespace PilotsDeck_FNX2PLD
{
    public class Program
    {
        public static readonly string FenixExecutable = Convert.ToString(ConfigurationManager.AppSettings["FenixExecutable"]) ?? "FenixSystem";
        public static readonly string logFilePath = Convert.ToString(ConfigurationManager.AppSettings["logFilePath"]) ?? "FNX2PLD.log";
        public static readonly string logLevel = Convert.ToString(ConfigurationManager.AppSettings["logLevel"]) ?? "Debug";
        public static readonly bool waitForConnect = Convert.ToBoolean(ConfigurationManager.AppSettings["waitForConnect"]);
        public static readonly int offsetBase = Convert.ToInt32(ConfigurationManager.AppSettings["offsetBase"], 16);
        public static readonly int updateIntervall = Convert.ToInt32(ConfigurationManager.AppSettings["updateIntervall"]);
        public static readonly string groupName = "FNX2PLD";

        private static MemoryScanner scanner;
        private static ElementManager elementManager;

        public static void Main()
        {
            try
            {
                //Init Prog, Open Process, Open FSUIPC
                if (!Initialize())
                    return;

                //Search Locations for Patterns
                scanner.SearchPatterns(elementManager.Patterns.Values.ToList());

                foreach (var pattern in elementManager.Patterns)
                {
                    if (pattern.Value.Location != 0)
                        Log.Information($"Program: Pattern <{pattern.Key}> is at Address 0x{pattern.Value.Location:X} ({pattern.Value.Location:d})");
                    else
                        Log.Error($"Program: Location for Pattern <{pattern.Key}> not found!");
                }


                //Main Loop
                CancellationToken cancellationToken = new CancellationToken();
                Stopwatch watch = new Stopwatch();
                int measures = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    watch.Start();

                    scanner.UpdateBuffers(elementManager.Patterns);
                    elementManager.GenerateValues();

                    watch.Stop();
                    measures++;
                    if (measures > 50)
                    {
                        Log.Logger.Debug($"Program: -------------------------------- Average elapsed Time for Reading and Updating Buffers: {string.Format("{0,3:F}", (watch.Elapsed.TotalMilliseconds) / measures)}ms --------------------------------");
                        measures = 0;
                        watch.Reset();
                    }

                    Thread.Sleep(updateIntervall);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Program: Critical Exception occured: {ex.Source} - {ex.Message}");
            }
        }

        private static bool Initialize()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day);
            if (logLevel == "Warning")
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == "Debug")
                loggerConfiguration.MinimumLevel.Debug();
            else
                loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Information($"FNX2PLD started! Log Level: {logLevel} Log File: {logFilePath}");

            if (!FSUIPCConnection.IsOpen && waitForConnect)
                IPCManager.WaitForConnection();
            else
                IPCManager.OpenSafeFSUIPC();

            Offset<byte> isMenu = new(groupName, 0x3365);
            Offset<Int16> isPaused = new(groupName, 0x0262);

            do
            {
                Log.Information($"Wating for MSFS/Fenix to become ready, sleeping 5s");
                Thread.Sleep(5000);
                FSUIPCConnection.Process(groupName);
            }
            while (!IPCManager.OpenSafeFSUIPC() || isMenu.Value != 0 || isPaused.Value != 0);

            if (!IPCManager.GetCurrentAircraft())
                return false;

            Process? fenixProc = null;

            while (fenixProc == null)
            {
                fenixProc = Process.GetProcessesByName(FenixExecutable).FirstOrDefault();
                if (fenixProc != null)
                    scanner = new MemoryScanner(fenixProc);
                else
                {
                    Log.Warning($"Could not find Process {FenixExecutable}, trying again in 5s");
                    Thread.Sleep(5000);
                }
            }
            Log.Information($"Waiting for User to click Ready to Fly, sleeping 20s");
            Thread.Sleep(20000);

            if (!scanner.IsInitialized())
            {
                Log.Error($"Could not open Process {FenixExecutable}!");
                return false;
            }

            Log.Information($"Initializing WASM Module");
            MSFSVariableServices.Init(Process.GetCurrentProcess().MainWindowHandle);
            MSFSVariableServices.LVARUpdateFrequency = 0;
            MSFSVariableServices.Start();
            if (!MSFSVariableServices.IsRunning)
            {
                Log.Error($"WASM Module is not running!");
                return false;
            }

            elementManager = new ElementManager();

            

            return true;
        }

        
    }
}