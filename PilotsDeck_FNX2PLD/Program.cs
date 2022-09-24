using FSUIPC;
using Serilog;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PilotsDeck_FNX2PLD
{
    public class Program
    {
        public static readonly string FenixExecutable = Convert.ToString(ConfigurationManager.AppSettings["FenixExecutable"]) ?? "FenixSystem";
        public static readonly string logFilePath = Convert.ToString(ConfigurationManager.AppSettings["logFilePath"]) ?? "FNX2PLD.log";
        public static readonly string logLevel = Convert.ToString(ConfigurationManager.AppSettings["logLevel"]) ?? "Debug";
        public static readonly bool waitForConnect = Convert.ToBoolean(ConfigurationManager.AppSettings["waitForConnect"]);
        public static readonly bool ignoreCurrentAC = Convert.ToBoolean(ConfigurationManager.AppSettings["ignoreCurrentAC"]);
        public static readonly int offsetBase = Convert.ToInt32(ConfigurationManager.AppSettings["offsetBase"], 16);
        public static readonly int updateIntervall = Convert.ToInt32(ConfigurationManager.AppSettings["updateIntervall"]);
        public static readonly int waitReady = Convert.ToInt32(ConfigurationManager.AppSettings["waitReady"]);
        public static readonly int waitTick = Convert.ToInt32(ConfigurationManager.AppSettings["waitTick"]);
        public static readonly string groupName = "FNX2PLD";

        private static MemoryScanner scanner;
        private static ElementManager elementManager;

        private static Offset<byte> isMenu;
        private static Offset<Int16> isPaused;

        public static void Main()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3);
            if (logLevel == "Warning")
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == "Debug")
                loggerConfiguration.MinimumLevel.Debug();
            else
                loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Information($"Program: FNX2PLD started! Log Level: {logLevel} Log File: {logFilePath}");

            try
            {
                //Init Prog, Open Process/FSUIPC, Wait for Connect
                if (!Initialize())
                    return;

                CancellationToken cancellationToken = new CancellationToken();

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Wait())
                    {
                        MainLoop(cancellationToken);
                        if (FSUIPCConnection.IsOpen)
                        {
                            Log.Logger.Information($"Program: Resetting Session (MainLoop stopped and FSUIPC Connected)");
                            Reset();
                        }
                    }
                    else if (!FSUIPCConnection.IsOpen)
                    {
                        Log.Logger.Error($"Program: FSUIPC Connection is closed - exiting.");
                        break;
                    }
                    else if (!IPCManager.IsAircraftFenix() && !ignoreCurrentAC)
                    {
                        Log.Logger.Warning($"Program: Loaded Aircraft is not a Fenix - exiting.");
                        break;
                    }
                    else
                    {
                        Log.Logger.Information($"Program: Resetting Session (WaitLoop failed)");
                        Reset();
                        Log.Information($"Program: Waiting {waitTick}s");
                        Thread.Sleep(waitTick * 1000);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Program: Critical Exception occured: {ex.Source} - {ex.Message}");
            }
        }

        private static void MainLoop(CancellationToken cancellationToken)
        {
            //Main Loop
            Stopwatch watch = new Stopwatch();
            int measures = 0;
            int averageTick = 150;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    watch.Start();

                    if (!scanner.UpdateBuffers(elementManager.Patterns))
                    {
                        Log.Logger.Error($"Program: UpdateBuffers() failed - Exiting");
                        break;
                    }
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
            catch
            {
                Log.Logger.Error($"Program: Critical Exception during MainLoop()");
            }
        }

        private static bool Wait()
        {
            bool startDirect = true;

            try
            {
                //Wait until Fenix is the current Aircraft for FSUIPC
                do
                {
                    if (!IPCManager.RefreshCurrentAircraft())
                    {
                        startDirect = false;
                        Log.Information($"Program: Wating for until Fenix is the loaded Aircraft and Sim is unpaused, sleeping for {waitTick * 4}s");
                        Thread.Sleep(waitTick * 1000 * 4);
                    }
                    if (!FSUIPCConnection.IsOpen)
                        return false;
                }
                while (!IPCManager.IsAircraftFenix() || isMenu.Value == 1 || isPaused.Value == 1);

                //Wait until the Fenix Executable is running
                Process? fenixProc = null;
                while (fenixProc == null && FSUIPCConnection.IsOpen)
                {
                    fenixProc = Process.GetProcessesByName(FenixExecutable).FirstOrDefault();
                    if (fenixProc != null)
                        scanner = new MemoryScanner(fenixProc);
                    else
                    {
                        startDirect = false;
                        Log.Warning($"Program: Could not find Process {FenixExecutable}, trying again in {waitTick * 2}s");
                        Thread.Sleep(waitTick * 1000 * 2);
                    }
                }

                //Delay Scanner Initialization until User clicked "Ready to Fly"
                if (!startDirect)
                {
                    Log.Information($"Program: Waiting for User to click Ready to Fly, sleeping {waitReady}s");
                    Thread.Sleep(waitReady * 1000);
                }

                if (!scanner.IsInitialized())
                {
                    Log.Error($"Program: Could not open Process {FenixExecutable}!");
                    return false;
                }

                //Start WASM and ElementManager
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

                //Search Memory Locations for Patterns
                scanner.SearchPatterns(elementManager.Patterns.Values.ToList());

                foreach (var pattern in elementManager.Patterns)
                {
                    if (pattern.Value.Location != 0)
                        Log.Information($"Program: Pattern <{pattern.Key}> is at Address 0x{pattern.Value.Location:X} ({pattern.Value.Location:d})");
                    else
                        Log.Error($"Program: Location for Pattern <{pattern.Key}> not found!");
                }

                return true;
            }
            catch
            {
                Log.Logger.Error($"Program: Critical Exception during Wait()");
            }

            return false;
        }

        private static bool Initialize()
        {
            if (!FSUIPCConnection.IsOpen && waitForConnect)
                IPCManager.WaitForConnection();
            else
            {
                if (!IPCManager.OpenSafeFSUIPC())
                    return false;
            }

            isMenu = new(0x3365);
            isPaused = new(0x0262);
            
            return true;
        }

        private static void Reset()
        {
            try
            {
                scanner = null;
                if (elementManager != null)
                    elementManager.Dispose();
                elementManager = null;
                if (FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Close();
                if (MSFSVariableServices.IsRunning)
                    MSFSVariableServices.Stop();
            }
            catch
            {
                Log.Logger.Error($"Program: Exception during Reset()");
            }
        }
    }
}