using System.Collections.ObjectModel;
using System.Windows.Input;
using Celery.Settings;

namespace Celery.ViewModel
{
    public class SettingsViewModel : Core.ViewModel
    {
        public ICommand CloseCommand { get; set; }
        public ObservableCollection<Setting> Settings { get; }

        public SettingsViewModel(ObservableCollection<Setting> settings)
        {
            Settings = settings;
        }
    }
}