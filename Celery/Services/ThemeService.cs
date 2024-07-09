using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Celery.Core;
using Celery.Properties;

namespace Celery.Services;

public interface IThemeService
{
    Dictionary<string, ResourceDictionary> Themes { get; }

    void SetTheme(string name);
}

public class ThemeService : ObservableObject, IThemeService
{
    public Dictionary<string, ResourceDictionary> Themes { get; }

    private ILoggerService LoggerService { get; }

    public ThemeService(ILoggerService loggerService)
    {
        LoggerService = loggerService;
        Themes = new Dictionary<string, ResourceDictionary>();
        Themes.Add("Default", Application.Current.Resources.MergedDictionaries[0]);

        string defaultMonaco = Path.Combine(Config.ThemesPath, "Default.json");
        if (!File.Exists(defaultMonaco))
            File.WriteAllBytes(defaultMonaco, Resources.DefaultMonacoTheme);
        
        // Load themes from %appdata%\Celery\themes
        foreach (string file in Directory.GetFiles(Config.ThemesPath, "*.xaml"))
        {
            using FileStream fs = new(file, FileMode.Open);
            try
            {
                ResourceDictionary rd = (ResourceDictionary)XamlReader.Load(fs);
                Themes.Add(Path.GetFileNameWithoutExtension(file), rd);
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex.Message);
                return;
            }
        }
    }

    public void SetTheme(string name)
    {
        Application.Current.Resources.MergedDictionaries.RemoveAt(0);
        Application.Current.Resources.MergedDictionaries.Insert(0, Themes[name]);

        // Apply the ace theme
        string acePath = Path.Combine(Config.BinPath, "Ace", "js", "ace");
        string templatePath = Path.Combine(acePath, "theme-template.js");
        if (File.Exists(templatePath))
        {
            try
            {
                string template = File.ReadAllText(templatePath);
                ResourceDictionary resources = Application.Current.Resources;
                template = template.Replace("{background}", ((Color)resources["BackgroundColor"]).ToString().Replace("#FF", "#"));
                template = template.Replace("{foreground}", ((Color)resources["ForegroundColor"]).ToString().Replace("#FF", "#"));
                template = template.Replace("{bordercolor}", ((Color)resources["BorderColor"]).ToString().Replace("#FF", "#"));
                template = template.Replace("{lightbordercolor}", ((Color)resources["LightBorderColor"]).ToString().Replace("#FF", "#"));
                template = template.Replace("{darkforeground}", ((Color)resources["DarkForegroundColor"]).ToString().Replace("#FF", "#"));
                template = template.Replace("{highlightcolor}", ((Color)resources["HighlightColor"]).ToString().Replace("#FF", "#"));
                template = template.Replace("{lightbackground}", ((Color)resources["LightBackgroundColor"]).ToString().Replace("#FF", "#"));
                File.WriteAllText(Path.Combine(acePath, "theme-celery.js"), template);
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex.Message);
            }
        }
        else
        {
            LoggerService.Error($"Couldn't find Ace theme template file: '{templatePath}'");
        }

        // Apply the monaco theme
        string monacoThemePath = Path.Combine(Config.ThemesPath, name + ".json");
        string themePath = Path.Combine(Config.BinPath, "Monaco", "assets", "theme.json");
        if (File.Exists(monacoThemePath))
            File.Copy(monacoThemePath, themePath, true);
        else
            LoggerService.Error($"Couldn't find Monaco theme file: '{monacoThemePath}'");
    }
}