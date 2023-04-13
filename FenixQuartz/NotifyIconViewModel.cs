using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System.Windows;

namespace FenixQuartz
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        [RelayCommand]
        public void ShowWindow()
        {
            if (App.devGUI)
            {
                if (!Application.Current.MainWindow.IsVisible)
                    Application.Current.MainWindow.Show(disableEfficiencyMode: true);
                else
                    Application.Current.MainWindow.Hide();
            }
        }

        [RelayCommand]
        public void RestartScanner()
        {
            App.RestartRequested = true;
        }

        [RelayCommand]
        public void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
