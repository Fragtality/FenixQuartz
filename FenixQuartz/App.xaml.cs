using H.NotifyIcon;
using Serilog;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FenixQuartz
{
    public partial class App : Application
    {
        public static bool devGUI;
        public static string FenixExecutable;
        public static string logFilePath;
        public static string logLevel;
        public static bool waitForConnect;
        public static int offsetBase;
        public static bool rawValues;
        public static bool useLvars;
        public static int updateIntervall;
        public static string altScaleDelim;
        public static bool addFcuMode;
        public static bool ooMode;
        public static string lvarPrefix;
        public static bool ignoreBatteries;
        public static bool perfCaptainSide;
        public static int perfButtonHold;
        public static string groupName = "FenixQuartz";

        public static new App Current => Application.Current as App;
        public static string ConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\FenixQuartz\FenixQuartz.config";
        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\FenixQuartz\bin";
        public static ConfigurationFile ConfigurationFile = new();

        public static bool CancellationRequested { get; set; } = false;
        public static bool RestartRequested { get; set; } = false;
        public static bool ServiceExited { get; set; } = false;

        private TaskbarIcon notifyIcon;
        public static QuartzService Service;

        protected static void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();
            devGUI = Convert.ToBoolean(ConfigurationFile.GetSetting("debugGUI", "true"));
            FenixExecutable = Convert.ToString(ConfigurationFile.GetSetting("FenixExecutable", "FenixSystem"));
            logFilePath = @"..\log\" + Convert.ToString(ConfigurationFile.GetSetting("logFilePath", "FenixQuartz.log"));
            logLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            waitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));
            offsetBase = Convert.ToInt32(ConfigurationFile.GetSetting("offsetBase", "0x5408"), 16);
            rawValues = Convert.ToBoolean(ConfigurationFile.GetSetting("rawValues", "false"));
            useLvars = Convert.ToBoolean(ConfigurationFile.GetSetting("useLvars", "false"));
            updateIntervall = Convert.ToInt32(ConfigurationFile.GetSetting("updateIntervall", "100"));
            altScaleDelim = Convert.ToString(ConfigurationFile.GetSetting("altScaleDelim", " "));
            addFcuMode = Convert.ToBoolean(ConfigurationFile.GetSetting("addFcuMode", "true"));
            ooMode = Convert.ToBoolean(ConfigurationFile.GetSetting("ooMode", "false"));
            lvarPrefix = Convert.ToString(ConfigurationFile.GetSetting("lvarPrefix", "FNX2PLD_"));
            ignoreBatteries = Convert.ToBoolean(ConfigurationFile.GetSetting("ignoreBatteries", "false"));
            perfCaptainSide = Convert.ToBoolean(ConfigurationFile.GetSetting("perfCaptainSide", "true"));
            perfButtonHold = Convert.ToInt32(ConfigurationFile.GetSetting("perfButtonHold", "1000"));
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.SetCurrentDirectory(AppDir);

            if (!File.Exists(ConfigFilePath))
            {
                ConfigFilePath = Directory.GetCurrentDirectory() + @"\FenixQuartz.config";
                if (!File.Exists(ConfigFilePath))
                {
                    MessageBox.Show("No Configuration File found! Closing ...", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
            }

            LoadConfiguration();
            InitLog();
            InitSystray();

            Service = new();
            Task.Run(Service.Run);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
            timer.Start();

            MainWindow = new MainWindow();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Log(LogLevel.Information, "App:OnExit", "FenixQuartz exiting ...");

            CancellationRequested = true;
            notifyIcon?.Dispose();
            base.OnExit(e);
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (ServiceExited)
            {
                Logger.Log(LogLevel.Information, "App:OnTick", "Received Signal that Service has exited");
                Current.Shutdown();
            }
        }

        protected static void InitLog()
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
            Logger.Log(LogLevel.Information, "App:InitLog", $"FenixQuartz started! Log Level: {logLevel} Log File: {logFilePath}");
        }

        protected void InitSystray()
        {
            Logger.Log(LogLevel.Information, "App:InitSystray", $"Creating SysTray Icon ...");
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.Icon = GetIcon("quartz.ico");
            notifyIcon.ForceCreate(false);
        }

        public static Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FenixQuartz.{filename}");
            return new Icon(stream);
        }
    }
}
