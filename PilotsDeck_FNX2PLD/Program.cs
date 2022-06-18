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
        public static readonly int waitReady = Convert.ToInt32(ConfigurationManager.AppSettings["waitReady"]);
        public static readonly int waitTick = Convert.ToInt32(ConfigurationManager.AppSettings["waitTick"]);
        public static readonly string groupName = "FNX2PLD";

        private static MemoryScanner scanner;
        private static ElementManager elementManager;

        public static void Main()
        {
            try
            {
                //Init Prog, Open Process/FSUIPC, Wait for Connect
                if (!Initialize())
                    return;

                //Search Memory Locations for Patterns
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
                int averageTick = 150;

                while (!cancellationToken.IsCancellationRequested)
                {
                    watch.Start();

                    scanner.UpdateBuffers(elementManager.Patterns);
                    elementManager.GenerateValues();

                    watch.Stop();
                    measures++;
                    if (measures > averageTick)
                    {
                        Log.Logger.Debug($"Program: -------------------------------- Average elapsed Time for Reading and Updating Buffers: {string.Format("{0,3:F}", (watch.Elapsed.TotalMilliseconds) / averageTick)}ms --------------------------------");
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
            Log.Information($"Program: FNX2PLD started! Log Level: {logLevel} Log File: {logFilePath}");

            if (!FSUIPCConnection.IsOpen && waitForConnect)
                IPCManager.WaitForConnection();
            else
                IPCManager.OpenSafeFSUIPC();

            Offset<byte> isMenu = new(0x3365);
            Offset<Int16> isPaused = new(0x0262);

            do
            {
                Log.Information($"Program: Wating for MSFS/Fenix to become ready, sleeping {waitTick}s");
                Thread.Sleep(waitTick * 1000);
                FSUIPCConnection.Process();
            }
            while (!IPCManager.GetCurrentAircraft() || isMenu.Value == 1 || isPaused.Value == 1);

            if (!IPCManager.GetCurrentAircraft())
            {
                Log.Error($"Program: MSFS now read but loaded Aircraft is not a Fenix! Closing ...");
                return false;
            }

            Process? fenixProc = null;

            while (fenixProc == null)
            {
                fenixProc = Process.GetProcessesByName(FenixExecutable).FirstOrDefault();
                if (fenixProc != null)
                    scanner = new MemoryScanner(fenixProc);
                else
                {
                    Log.Warning($"Program: Could not find Process {FenixExecutable}, trying again in {waitTick}s");
                    Thread.Sleep(waitTick * 1000);
                }
            }
            Log.Information($"Program: Waiting for User to click Ready to Fly, sleeping {waitReady}s");
            Thread.Sleep(waitReady * 1000);

            if (!scanner.IsInitialized())
            {
                Log.Error($"Program: Could not open Process {FenixExecutable}!  Closing ...");
                return false;
            }

            Log.Information($"Program: Initializing WASM Module");
            MSFSVariableServices.Init(Process.GetCurrentProcess().MainWindowHandle);
            MSFSVariableServices.LVARUpdateFrequency = 0;
            MSFSVariableServices.Start();
            if (!MSFSVariableServices.IsRunning)
            {
                Log.Error($"Program: WASM Module is not running! Closing ...");
                return false;
            }

            elementManager = new ElementManager();
            
            return true;
        }
    }
}