using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace FenixQuartz
{
    public partial class NotifyIconViewModel : ObservableObject
    {
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
