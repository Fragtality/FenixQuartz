using System;
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

                isSpdManaged.Content = manager.isSpdManaged;
                fcuSpdDashed.Content = values["fcuSpdDashed"].GetValue() ?? 0;
                fcuSpd.Content = values["fcuSpd"].GetValue() ?? 0;
                fcuSpdManaged.Content = values["fcuSpdManaged"].GetValue() ?? 0;

                isHdgManaged.Content = manager.isHdgManaged;
                fcuHdgDashed.Content = values["fcuHdgDashed"].GetValue() ?? 0;
                fcuHdg.Content = values["fcuHdg"].GetValue() ?? 0;
                fcuHdgManaged.Content = values["fcuHdgManaged"].GetValue() ?? 0;

                isAltManaged.Content = manager.isAltManaged;
                fcuAlt.Content = values["fcuAlt"].GetValue() ?? 0;

                isAltVsMode.Content = manager.isAltVsMode;
                fcuVsDashed.Content = values["fcuVsDashed"].GetValue() ?? 0;
                fcuVs.Content = values["fcuVs"].GetValue() ?? 0;
                fcuVsManaged.Content = values["fcuVsManaged"].GetValue() ?? 0;

                isisStd1.Content = values["isisStd1"].GetValue() ?? 0;
                isisBaro1.Content = values["isisBaro1"].GetValue() ?? 0;
                isisStd2.Content = values["isisStd2"].GetValue() ?? 0;
                isisBaro2.Content = values["isisBaro2"].GetValue() ?? 0;
                isisStd3.Content = values["isisStd3"].GetValue() ?? 0;
                isisBaro3.Content = values["isisBaro3"].GetValue() ?? 0;

                com1Active.Content = values["com1Active"].GetValue() ?? 0;
                com1Standby.Content = values["com1Standby"].GetValue() ?? 0;
                com2Active.Content = values["com2Active"].GetValue() ?? 0;
                com2Standby.Content = values["com2Standby"].GetValue() ?? 0;

                xpdrDisplay.Content = values["xpdrDisplay"].GetValue() ?? 0;
                xpdrInput.Content = values["xpdrInput"].GetValue() ?? 0;
                xpdrDigits.Content = values["xpdrDigits"].GetValue() ?? 0;

                if (!App.ignoreBatteries)
                {
                    bat1Display.Content = values["bat1Display"].GetValue() ?? 0;
                    bat1Display21.Content = values["bat1Display21"].GetValue() ?? 0;
                    bat2Display1.Content = values["bat2Display1"].GetValue() ?? 0;
                    bat2Display2.Content = values["bat2Display2"].GetValue() ?? 0;
                }

                rudderDisplay1.Content = values["rudderDisplay1"].GetValue() ?? 0;
                rudderDisplay2.Content = values["rudderDisplay2"].GetValue() ?? 0;
                rudderDisplay3.Content = values["rudderDisplay3"].GetValue() ?? 0;
                rudderDisplay4.Content = values["rudderDisplay4"].GetValue() ?? 0;
                rudderDisplay5.Content = values["rudderDisplay5"].GetValue() ?? 0;

                clockCHR.Content = values["clockCHR"].GetValue() ?? 0;
                clockET.Content = values["clockET"].GetValue() ?? 0;

                //speedV1_1.Content = values["speedV1-1"].GetValue() ?? 0;
                //speedVR_1.Content = values["speedVR-1"].GetValue() ?? 0;
                //speedV2_1.Content = values["speedV2-1"].GetValue() ?? 0;
                //speedV1_2.Content = values["speedV1-2"].GetValue() ?? 0;
                //speedVR_2.Content = values["speedVR-2"].GetValue() ?? 0;
                //speedV2_2.Content = values["speedV2-2"].GetValue() ?? 0;
                //speedV1_3.Content = values["speedV1-3"].GetValue() ?? 0;
                //speedVR_3.Content = values["speedVR-3"].GetValue() ?? 0;
                //speedV2_3.Content = values["speedV2-3"].GetValue() ?? 0;
                //speedV1_4.Content = values["speedV1-4"].GetValue() ?? 0;
                //speedVR_4.Content = values["speedVR-4"].GetValue() ?? 0;
                //speedV2_4.Content = values["speedV2-4"].GetValue() ?? 0;
                //speedV1_5.Content = values["speedV1-5"].GetValue() ?? 0;
                //speedVR_5.Content = values["speedVR-5"].GetValue() ?? 0;
                //speedV2_5.Content = values["speedV2-5"].GetValue() ?? 0;
                //speedV1_6.Content = values["speedV1-6"].GetValue() ?? 0;
                //speedVR_6.Content = values["speedVR-6"].GetValue() ?? 0;
                //speedV2_6.Content = values["speedV2-6"].GetValue() ?? 0;

                //speedVAPP_1.Content = values["speedVAPP-1"].GetValue() ?? 0;
                //speedVAPP_2.Content = values["speedVAPP-2"].GetValue() ?? 0;
                //speedVAPP_3.Content = values["speedVAPP-3"].GetValue() ?? 0;
                //speedVAPP_4.Content = values["speedVAPP-4"].GetValue() ?? 0;
                //speedVAPP_5.Content = values["speedVAPP-5"].GetValue() ?? 0;
                //speedVAPP_5.Content = values["speedVAPP-6"].GetValue() ?? 0;
                speedV1.Content = values["speedV1"].GetValue() ?? 0;
                speedVR.Content = values["speedVR"].GetValue() ?? 0;
                speedV2.Content = values["speedV2"].GetValue() ?? 0;

                speedVAPP.Content = values["speedVAPP"].GetValue() ?? 0;
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
