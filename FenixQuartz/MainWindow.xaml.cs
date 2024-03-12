using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace FenixQuartz
{
    public partial class MainWindow : Window
    {
        protected DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyVersion = assemblyVersion[0..assemblyVersion.LastIndexOf('.')];
            Title += " (" + assemblyVersion + ")";

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += OnTick;
        }


        protected void OnTick(object sender, EventArgs e)
        {
            if (App.Service != null && App.Service.elementManager != null && App.Service.elementManager.MemoryValues != null)
            {
                var manager = App.Service.elementManager;
                var values = App.Service.elementManager.MemoryValues;

                isisStd1.Content = values["isisStd1"].GetValue() ?? 0;
                isisBaro1.Content = values["isisBaro1"].GetValue() ?? 0;
                isisStd2.Content = values["isisStd2"].GetValue() ?? 0;
                isisBaro2.Content = values["isisBaro2"].GetValue() ?? 0;
                isisStd3.Content = values["isisStd3"].GetValue() ?? 0;
                isisBaro3.Content = values["isisBaro3"].GetValue() ?? 0;

                xpdrInput.Content = values["xpdrInput"].GetValue() ?? 0;

                rudderDashed1.Content = values["rudderDashed1"].GetValue() ?? 0;
                rudderDashed2.Content = values["rudderDashed2"].GetValue() ?? 0;

                speedV1.Content = manager.speedV1;
                speedVR.Content = manager.speedVR;
                speedV2.Content = manager.speedV2;
                toFlex.Content = manager.toFlex;
            }
        }

        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                timer.Stop();
            }
            else
            {
                timer.Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
