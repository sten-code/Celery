using System;
using System.IO;
using System.Reflection;

namespace Celery
{
    public static class Config
    {
        public static string ApplicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ScriptsPath = Path.Combine(ApplicationPath, "Scripts");
        public static string BinPath = Path.Combine(ApplicationPath, "bin");

        public static string Roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string SettingsPath = Path.Combine(Roaming, "Celery");
        public static string UpdaterPath = Path.Combine(SettingsPath, "CeleryUpdater.exe");
        public static string SettingsFilePath = Path.Combine(SettingsPath, "settings.json");
        public static string ThemesPath = Path.Combine(SettingsPath, "Themes");
        public static string TabsPath = Path.Combine(SettingsPath, "Tabs");

        public static string CeleryTemp = Path.Combine(Path.GetTempPath(), "celery");
        public static string CeleryHome = Path.Combine(CeleryTemp, "celeryhome.txt");
        public static string CeleryDir = Path.Combine(CeleryTemp, "celerydir.txt");
    }
}
