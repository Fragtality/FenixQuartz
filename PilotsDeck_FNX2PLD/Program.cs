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
        public static readonly bool rawValues = Convert.ToBoolean(ConfigurationManager.AppSettings["rawValues"]);
        public static readonly int updateIntervall = Convert.ToInt32(ConfigurationManager.AppSettings["updateIntervall"]);
        public static readonly string groupName = "FNX2PLD";

        private static MemoryScanner? scanner = null;
        private static ElementManager? elementManager = null;
        private static bool cancelRequested = false;
        private static CancellationToken cancellationToken;
        private static bool wasmInitialized = false;

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
            Log.Information($"-----------------------------------------------------------------------");
            Log.Information($"Program: FNX2PLD started! Log Level: {logLevel} Log File: {logFilePath}");

            try
            {
                CancellationTokenSource cancellationTokenSource = new();
                cancellationToken = cancellationTokenSource.Token;

                while (!cancellationToken.IsCancellationRequested && !cancelRequested)
                {
                    if (Wait() && InitializeSession())
                    {
                        MainLoop();
                    }
                    else
                    {
                        if (!RetryPossible())
                        {
                            cancelRequested = true;
                            Log.Logger.Error($"Program: Session aborted, Retry not possible - exiting Program");
                        }
                        else
                        {
                            Reset();
                            Log.Logger.Information($"Program: Session aborted, Retry possible - Waiting for new Session");
                        }
                    }
                }

                Close();
                
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Program: Critical Exception occured: {ex.Source} - {ex.Message}");
            }

            Log.Information($"Program: FNX2PLD terminated.");
        }

        private static bool Wait()
        {
            if (!IPCManager.WaitForSimulator(cancellationToken))
                return false;

            if (!IPCManager.WaitForConnection(cancellationToken))
                return false;

            if (!IPCManager.WaitForFenixAircraft(cancellationToken))
                return false;

            if (!IPCManager.WaitForFenixBinary(cancellationToken))
                return false;

            if (!IPCManager.WaitForSessionReady(cancellationToken))
                return false;

            return true;
        }

        private static bool RetryPossible()
        {
            return IPCManager.IsSimRunning() && FSUIPCConnection.IsOpen;
        }

        private static void Reset()
        {
            try
            {
                scanner = null;
                if (elementManager != null)
                    elementManager.Dispose();
                elementManager = null;
            }
            catch
            {
                Log.Logger.Error($"Program: Exception during Reset()");
            }
        }

        private static void Close()
        {
            Reset();
            IPCManager.CloseSafe();
        }

        private static bool InitializeSession()
        {
            try
            {
                Process? fenixProc = Process.GetProcessesByName(FenixExecutable).FirstOrDefault();
                if (fenixProc != null)
                {
                    scanner = new MemoryScanner(fenixProc);
                }
                else
                {
                    Log.Logger.Error($"InitializeSession: Fenix Process is null!");
                    return false;
                }

                if (!InitWASM())
                {
                    Log.Logger.Error($"InitializeSession: Intialization of WASM failed!");
                    return false;
                }

                elementManager = new ElementManager();
                scanner.SearchPatterns(elementManager.MemoryPatterns.Values.ToList());

                return true;
            }
            catch
            {
                Log.Logger.Error($"InitializeSession: Exception during Intialization!");
                return false;
            }
        }

        private static bool InitWASM()
        {
            if (!wasmInitialized)
            {
                IPCManager.InitWASM();
                wasmInitialized = true;
            }
            else
            {
                MSFSVariableServices.Start();
            }

            return MSFSVariableServices.IsRunning;
        }

        private static void MainLoop()
        {
            if (scanner == null || elementManager == null)
                throw new ArgumentException("MainLoop: MemoryScanner or ElementManager are null!");

            elementManager.PrintReport();
            //Main Loop
            Stopwatch watch = new Stopwatch();
            int measures = 0;
            int averageTick = 150;

            try
            {
                while (!cancellationToken.IsCancellationRequested && IPCManager.IsProcessRunning(FenixExecutable))
                {
                    watch.Start();

                    if (!scanner.UpdateBuffers(elementManager.MemoryValues))
                    {
                        Log.Logger.Error($"MainLoop: UpdateBuffers() failed - Exiting");
                        break;
                    }
                    elementManager.GenerateValues();

                    watch.Stop();
                    measures++;
                    if (measures > averageTick)
                    {
                        Log.Logger.Debug($"MainLoop: -------------------------------- Average elapsed Time for Reading and Updating Buffers: {string.Format("{0,3:F}", (watch.Elapsed.TotalMilliseconds) / averageTick)}ms --------------------------------");
                        Log.Logger.Debug($"rudderDisplay1: {elementManager.MemoryValues["rudderDisplay1"].GetValue()} (Tiny: {elementManager.MemoryValues["rudderDisplay1"].IsTinyValue()})");
                        Log.Logger.Debug($"rudderDisplay2: {elementManager.MemoryValues["rudderDisplay2"].GetValue()} (Tiny: {elementManager.MemoryValues["rudderDisplay2"].IsTinyValue()})");
                        Log.Logger.Debug($"rudderDisplay3: {elementManager.MemoryValues["rudderDisplay3"].GetValue()} (Tiny: {elementManager.MemoryValues["rudderDisplay3"].IsTinyValue()})");
                        measures = 0;
                        watch.Reset();
                    }

                    Thread.Sleep(updateIntervall);
                }

                Log.Logger.Information($"Program: MainLoop ended");
            }
            catch
            {
                Log.Logger.Error($"Program: Critical Exception during MainLoop()");
            }
        }
    }
}