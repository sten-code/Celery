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
    }
}
