using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Celery.Core;
using Celery.Settings;
using Newtonsoft.Json;

namespace Celery.Services;

public class SettingsEventArgs : EventArgs
{
    public Setting Setting;
}

public interface ISettingsService
{
    event EventHandler<SettingsEventArgs> OnSettingChanged;

    void Load();

    T GetSetting<T>(string id);
}

public class SettingsService : ObservableObject, ISettingsService
{
    public event EventHandler<SettingsEventArgs> OnSettingChanged;

    private ObservableCollection<Setting> Settings { get; }
    private IThemeService ThemeService { get; }
    private ITabSavingService TabSavingService { get; }
    private Dictionary<string, object> FileData { get; set; }

    public SettingsService(ObservableCollection<Setting> settings, IThemeService themeService, ITabSavingService tabSavingService)
    {
        ThemeService = themeService;
        TabSavingService = tabSavingService;
        Settings = settings;
        Settings.Add(new BooleanSetting("Background Blur", "background_blur", true, BooleanSettingChanged));
        Settings.Add(new BooleanSetting("Top Most", "topmost", false, BooleanSettingChanged));
        Settings.Add(new ChoiceSetting("Theme", "theme", themeService.Themes.Keys.ToList(), 0, RestartOnChanged));
        Settings.Add(new ChoiceSetting("Editor", "editor", ["Monaco", "Ace"], 0, RestartOnChanged));
        Load();
        ThemeService.SetTheme(GetSetting<string>("theme"));
    }

    private async void RestartOnChanged(ChoiceSetting setting, string value)
    {
        // Save the setting
        ChoiceSettingChanged(setting, value);

        // Save the tabs before restarting
        // If App.Exit() is used, it will first start Celery, then exit and save.
        // If it takes too long to save it won't load the tabs when it starts.
        await TabSavingService.Save();

        // Restart Celery
        Process.Start(Assembly.GetExecutingAssembly().Location);
        Application.Current.Shutdown();
    }

    private void BooleanSettingChanged(BooleanSetting setting, bool value)
    {
        if (OnSettingChanged == null)
            return;

        OnSettingChanged(this, new SettingsEventArgs
        {
            Setting = setting
        });

        Save(setting.Id, setting.GetValue());
    }

    private void ChoiceSettingChanged(ChoiceSetting setting, string value)
    {
        if (OnSettingChanged == null)
            return;

        OnSettingChanged(this, new SettingsEventArgs
        {
            Setting = setting
        });

        Save(setting.Id, setting.GetValue());
    }

    private void Save(string id, object value)
    {
        FileData[id] = value;
        string json = JsonConvert.SerializeObject(FileData);
        File.WriteAllText(Config.SettingsFilePath, json);
    }

    public void Load()
    {
        if (!File.Exists(Config.SettingsFilePath))
            File.WriteAllText(Config.SettingsFilePath, "{}");

        string json = File.ReadAllText(Config.SettingsFilePath);
        FileData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        foreach (Setting setting in Settings)
        {
            if (!FileData.TryGetValue(setting.Id, out object value))
            {
                Save(setting.Id, setting.GetValue());
                continue;
            }

            setting.SetValue(value);
        }
    }

    public T GetSetting<T>(string id)
    {
        return (T)Settings.FirstOrDefault(x => x.Id == id)?.GetValue();
    }
}