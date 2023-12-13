using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Installer
{
    public partial class InstallerWindow : Window
    {
        private InstallerWorker worker;
        private Queue<string> messageQueue;
        private DispatcherTimer timer;
        private bool workerHasFinished = false;

        public InstallerWindow()
        {
            InitializeComponent();

            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyVersion = assemblyVersion.Substring(0, assemblyVersion.LastIndexOf('.'));
            Title += "  (" + assemblyVersion + ")";

            messageQueue = new Queue<string>();
            worker = new InstallerWorker(messageQueue);
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            timer.Tick += OnTick;
        }

        protected void OnTick(object sender, EventArgs e)
        {
            while (messageQueue.Count > 0)
            {
                txtMessages.Text += messageQueue.Dequeue().ToString() + "\n";
            }
            if (!worker.IsRunning)
            {
                timer.Stop();
                workerHasFinished = true;
                if (worker.HasError)
                {
                    lblResult.Content = "ERROR during Installation!";
                    lblResult.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    lblResult.Content = "FINISHED successfully!";
                    lblResult.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    lblAvWarning.Visibility = Visibility.Visible;
                    lblRebootWarning.Visibility = Visibility.Visible;
                }
                btnInstall.Content = "Close";
                btnInstall.IsEnabled = true;
                Activate();
            }
        }

        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (!workerHasFinished)
            {
                if (InstallerFunctions.GetProcessRunning("FenixQuartz"))
                {
                    MessageBox.Show("Please stop FenixQuartz and try again.", "FenixQuartz is running!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                chkDesktopLink_Click(null, null);
                radio_ClickAutoStart(null, null);
                radio_ClickConfig(null, null);

                btnInstall.IsEnabled = false;
                timer.Start();
                Task.Run(worker.Run);
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        private void chkDesktopLink_Click(object sender, RoutedEventArgs e)
        {
            worker.CfgDesktopLink = chkDesktopLink.IsChecked ?? false;
        }

        private void radio_ClickAutoStart(object sender, RoutedEventArgs e)
        {
            if (radioNone.IsChecked == true)
                worker.CfgAutoStart = AutoStart.NONE;
            else if (radioFsuipc.IsChecked == true)
                worker.CfgAutoStart = AutoStart.FSUIPC;
            else if (radioExe.IsChecked == true)
                worker.CfgAutoStart = AutoStart.EXE;
        }

        private void radio_ClickConfig(object sender, RoutedEventArgs e)
        {
            if (radioConfNo.IsChecked == true)
                worker.CfgConfigFile = ConfigFile.NOCHANGE;
            else if (radioConfString.IsChecked == true)
                worker.CfgConfigFile = ConfigFile.STRING;
            else if (radioConfNumOffset.IsChecked == true)
                worker.CfgConfigFile = ConfigFile.NUMOFFSET;
            else if (radioConfNumLvar.IsChecked == true)
                worker.CfgConfigFile = ConfigFile.NUMLVAR;
        }
    }
}
