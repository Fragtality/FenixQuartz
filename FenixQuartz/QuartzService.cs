using FSUIPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace FenixQuartz
{
    public class QuartzService
    {
        public ElementManager elementManager = null;
        public List<OutputDefinition> Definitions = null;

        public void Run()
        {           
            try
            {
                WriteAssignmentFile();

                Logger.Log(LogLevel.Information, "QuartzService:Run", $"Entering Service Loop ...");
                while (!App.CancellationRequested)
                {
                    if (Wait() && InitializeSession())
                    {
                        ServiceLoop();
                        Reset();
                        if (App.RestartRequested)
                        {
                            Logger.Log(LogLevel.Information, "QuartzService:Run", $"Restart requested");
                            App.RestartRequested = false;
                        }
                    }
                    else
                    {
                        if (!RetryPossible())
                        {
                            App.CancellationRequested = true;
                            App.ServiceExited = true;
                            Logger.Log(LogLevel.Error, "QuartzService:Run", $"Session aborted, Retry not possible - exiting Program");
                        }
                        else
                        {
                            Reset();
                            Logger.Log(LogLevel.Information, "QuartzService:Run", $"Session aborted, Retry possible - Waiting for new Session");
                        }
                    }
                }

                Close();
                
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "QuartzService:Run", $"Critical Exception occured: {ex.Source} - {ex.Message}");
            }
        }

        private bool Wait()
        {
            if (!IPCManager.WaitForSimulator())
                return false;

            if (!IPCManager.WaitForConnection())
                return false;

            if (!IPCManager.WaitForFenixBinary())
                return false;

            if (!IPCManager.WaitForSessionReady())
                return false;

            return true;
        }

        private static bool RetryPossible()
        {
            return IPCManager.IsSimRunning();
        }

        private void Reset()
        {
            try
            {
                Logger.Log(LogLevel.Information, "QuartzService:Reset", $"Resetting Session");
                IPCManager.SimConnect?.Disconnect();
                IPCManager.SimConnect = null;
                if (!App.useLvars && FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Close();
                elementManager?.Dispose();
                elementManager = null;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "QuartzService:Reset", $"Exception during Reset ({ex.GetType()} {ex.Message})");
            }
        }

        private void Close()
        {
            Reset();
            IPCManager.CloseSafe();
        }

        private bool InitializeSession()
        {
            try
            {
                elementManager = new ElementManager(Definitions);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "QuartzService:InitializeSession", $"Exception during Intialization {ex.GetType()} {ex.Message})");
                return false;
            }
        }

        private void ServiceLoop()
        {
            if (elementManager == null)
                throw new ArgumentException("ServiceLoop: ElementManager is null");

            elementManager.PrintReport();
            //Service Loop
            Stopwatch watch = new();
            int measures = 0;
            int averageTick = 300;

            try
            {
                Thread.Sleep(300);
                while (!App.CancellationRequested && !App.RestartRequested && IPCManager.IsProcessRunning(App.FenixExecutable) && IPCManager.IsSimRunning())
                {
                    watch.Start();

                    if (!elementManager.GenerateValues())
                    {
                        Logger.Log(LogLevel.Error, "QuartzService:ServiceLoop", $"GenerateValues() failed");
                        break;
                    }

                    if (App.useLvars && measures % 50 == 0 || !App.useLvars && measures % 1000 == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }

                    watch.Stop();
                    measures++;
                    if (measures > averageTick)
                    {
                        //Logger.Log(LogLevel.Debug, "QuartzService:ServiceLoop", $"Average elapsed Time for Reading and Updating Buffers: {string.Format("{0,3:F}", (watch.Elapsed.TotalMilliseconds) / averageTick)}ms");
                        measures = 0;
                        watch.Reset();
                    }

                    Thread.Sleep(App.updateIntervall);
                }

                Logger.Log(LogLevel.Information, "QuartzService:ServiceLoop", $"ServiceLoop ended");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "QuartzService:InitializeSession", $"Exception during ServiceLoop {ex.GetType()} {ex.Message})");
            }
        }

        public void WriteAssignmentFile()
        {
            Logger.Log(LogLevel.Information, "QuartzService:WriteAssignmentFile", $"Writing Assignments.txt File ...");
            Definitions = OutputDefinition.CreateDefinitions();
            StringBuilder output = new();

            foreach(var value in Definitions)
            {
                output.AppendLine(value.ToString());
            }

            output.AppendLine("");
            output.AppendLine("TO Speeds (always L-Vars):");
            output.AppendLine(App.lvarPrefix + "speedV1");
            output.AppendLine(App.lvarPrefix + "speedVR");
            output.AppendLine(App.lvarPrefix + "speedV2");
            output.AppendLine(App.lvarPrefix + "toFlex");

            File.WriteAllText("..\\Assignments.txt", output.ToString());
        }
    }
}