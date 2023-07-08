using System;
using System.IO;
using System.Reflection;

namespace Celery
{
    public static class Config
    {
        // Version template: <release type (alpha | beta | release)>.<standard update>.<bug fixes/small changes>
        public static readonly Version Version = new Version("1.2.6");

        // All local folders
        public static readonly string ApplicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string ScriptsPath = Path.Combine(ApplicationPath, "Scripts");
        public static readonly string BinPath = Path.Combine(ApplicationPath, "bin");

        // All files and folders inside the %appdata%\Celery folder
        public static readonly string CeleryAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Celery");
        public static readonly string UpdaterPath = Path.Combine(CeleryAppDataPath, "CeleryUpdater.exe");
        public static readonly string SettingsFilePath = Path.Combine(CeleryAppDataPath, "settings.json");
        public static readonly string ThemesPath = Path.Combine(CeleryAppDataPath, "Themes");
        public static readonly string TabsPath = Path.Combine(CeleryAppDataPath, "Tabs");

        // All files inside the %temp%\celery folder
        public static readonly string CeleryTempPath = Path.Combine(Path.GetTempPath(), "celery");
        public static readonly string CeleryHome = Path.Combine(CeleryTempPath, "celeryhome.txt");
        public static readonly string CeleryDir = Path.Combine(CeleryTempPath, "celerydir.txt");
    }
}
