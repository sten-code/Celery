using System;
using System.IO;
using System.Reflection;

namespace Celery
{
    public static class Config
    {
        // Version template: <release type (alpha | beta | release)>.<standard update>.<bug fixes/small changes>
        public static readonly Version Version = new("2.0.3");

        // All local folders
        public static readonly string ApplicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string InjectorPath = Path.Combine(ApplicationPath, "CeleryInject.exe");
        public static readonly string ScriptsPath = Path.Combine(ApplicationPath, "scripts");
        public static readonly string BinPath = Path.Combine(ApplicationPath, "bin");
        public static readonly string AcePath = Path.Combine(BinPath, "Ace");
        public static readonly string MonacoPath = Path.Combine(BinPath, "Monaco");
        public static readonly string LspPath = Path.Combine(BinPath, "lsp");

        // All files and folders inside the %appdata%\Celery folder
        public static readonly string CeleryAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Celery");
        public static readonly string UpdaterPath = Path.Combine(CeleryAppDataPath, "CeleryUpdater.exe");
        public static readonly string SettingsFilePath = Path.Combine(CeleryAppDataPath, "settings.json");
        public static readonly string ThemesPath = Path.Combine(CeleryAppDataPath, "Themes");
        public static readonly string TabsPath = Path.Combine(CeleryAppDataPath, "Tabs");

        // All files inside the %temp%\celery folder
        public static readonly string CeleryTempPath = Path.Combine(Path.GetTempPath(), "celery");
        public static readonly string CeleryScriptFile = Path.Combine(CeleryTempPath, "myfile.txt");
        public static readonly string CeleryHomeFile = Path.Combine(CeleryTempPath, "celeryhome.txt");

        // Urls
        public static readonly string GitHubUrl = "https://api.github.com/repos/sten-code/Celery";
        public static readonly string GitHubReleasesUrl = $"{GitHubUrl}/releases";
        public static readonly string GitHubLatestReleaseUrl = $"{GitHubReleasesUrl}/latest";

        public static readonly byte[] Ace = Properties.Resources.Ace;
        public static readonly byte[] CeleryUpdater = Properties.Resources.CeleryUpdater;
        public static readonly byte[] Monaco = Properties.Resources.Monaco;
        public static readonly byte[] Lsp = Properties.Resources.lsp;
    }
}