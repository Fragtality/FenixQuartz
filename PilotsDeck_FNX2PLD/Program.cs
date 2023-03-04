using FSUIPC;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
        public static readonly bool useLvars = Convert.ToBoolean(ConfigurationManager.AppSettings["useLvars"]);
        public static readonly int updateIntervall = Convert.ToInt32(ConfigurationManager.AppSettings["updateIntervall"]);
        public static readonly string altScaleDelim = Convert.ToString(ConfigurationManager.AppSettings["altScaleDelim"]) ?? " ";
        public static readonly bool addFcuMode = Convert.ToBoolean(ConfigurationManager.AppSettings["addFcuMode"]);
        
        public static readonly string groupName = "FNX2PLD";
        public static readonly string lvarPrefix = "FNX2PLD_";

        //private static MemoryScanner scanner = null;
        private static ElementManager elementManager = null;
        private static bool cancelRequested = false;
        private static CancellationToken cancellationToken;
        public static List<OutputDefinition> Definitions = null;


        public static void Main()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}");
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
                WriteAssignmentFile();

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
                IPCManager.SimConnect?.Disconnect();
                IPCManager.SimConnect = null;
                //scanner = null;
                elementManager?.Dispose();
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
                //Process fenixProc = Process.GetProcessesByName(FenixExecutable).FirstOrDefault();
                //if (fenixProc != null)
                //{
                //    scanner = new MemoryScanner(fenixProc);
                //}
                //else
                //{
                //    Log.Logger.Error($"InitializeSession: Fenix Process is null!");
                //    return false;
                //}

                elementManager = new ElementManager();
                //scanner.SearchPatterns(elementManager.MemoryPatterns.Values.ToList());

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"InitializeSession: Exception during Intialization! ({ex.Message})");
                return false;
            }
        }

        private static void MainLoop()
        {
            if (elementManager == null)
                throw new ArgumentException("MainLoop: MemoryScanner or ElementManager are null!");

            elementManager.PrintReport();
            //Main Loop
            Stopwatch watch = new();
            int measures = 0;
            int averageTick = 300;

            try
            {
                while (!cancellationToken.IsCancellationRequested && IPCManager.IsProcessRunning(FenixExecutable) && IPCManager.IsSimRunning())
                {
                    watch.Start();

                    //if (!scanner.UpdateBuffers(elementManager.MemoryValues))
                    //{
                    //    Log.Logger.Error($"MainLoop: UpdateBuffers() failed - Exiting");
                    //    break;
                    //}
                    if (!elementManager.GenerateValues())
                    {
                        Log.Logger.Error($"MainLoop: GenerateValues() failed - Exiting");
                        break;
                    }
                    ;

                    watch.Stop();
                    measures++;
                    if (measures > averageTick)
                    {
                        Log.Logger.Debug($"MainLoop: -------------------------------- Average elapsed Time for Reading and Updating Buffers: {string.Format("{0,3:F}", (watch.Elapsed.TotalMilliseconds) / averageTick)}ms --------------------------------");
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

        public static void WriteAssignmentFile()
        {
            Definitions = OutputDefinition.CreateDefinitions();
            StringBuilder output = new();

            foreach(var value in Definitions)
            {
                output.AppendLine(value.ToString());
            }

            File.WriteAllText("Assignments.txt", output.ToString());
        }
    }
}