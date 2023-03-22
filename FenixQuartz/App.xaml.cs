using H.NotifyIcon;
using Serilog;
using System;
using System.Configuration;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FenixQuartz
{
    public partial class App : Application
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
        public static readonly bool ooMode = Convert.ToBoolean(ConfigurationManager.AppSettings["ooMode"]);
        public static readonly string lvarPrefix = Convert.ToString(ConfigurationManager.AppSettings["lvarPrefix"]);
        public static readonly string groupName = "FenixQuartz";

        public static bool CancellationRequested { get; set; } = false;
        public static bool RestartRequested { get; set; } = false;
        public static bool ServiceExited { get; set; } = false;

        private TaskbarIcon notifyIcon;
        private QuartzService Service;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CancellationRequested = true;
            notifyIcon?.Dispose();
            base.OnExit(e);

            Logger.Log(LogLevel.Information, "App:OnExit", "FenixQuartz exiting ...");
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (ServiceExited)
                Current.Shutdown();
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
            notifyIcon.ForceCreate();
        }

        public static Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FenixQuartz.{filename}");
            return new Icon(stream);
        }
    }
}
