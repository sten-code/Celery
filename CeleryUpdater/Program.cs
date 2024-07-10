using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CeleryUpdater
{
    public static class Program
    {
        public async static Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"{args.Length} arguments given, 2 were expected.");
                return;
            }

            string downloadUrl = args[0];
            string downloadPath = args[1];
            string[] whitelistedDirs =
            [
                "scripts"
            ];

            // The directory isn't immediately after accessible after killing the program so just try to delete it until its allowed
            Console.WriteLine("Clearing directory...");
            while (Directory.GetDirectories(downloadPath).Length != whitelistedDirs.Length || Directory.GetFiles(downloadPath).Length != 0)
            {
                try
                {
                    foreach (string dir in Directory.GetDirectories(downloadPath))
                        if (!whitelistedDirs.Contains(Path.GetFileName(dir)))
                            Directory.Delete(dir, true);
                    foreach (string file in Directory.GetFiles(downloadPath))
                        File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.Clear();
            Console.WriteLine("Downloading...");
            IProgress<double> downloadBar = new ProgressBar();
            using HttpClient client = new();
            using MemoryStream memStream = new(8000000); // 8MB
            await client.DownloadAsync(downloadUrl, memStream, downloadBar);
            memStream.Position = 0;

            Console.Clear();
            Console.WriteLine("Extracting...");
            IProgress<double> extractBar = new ProgressBar();
            await Task.Run(() =>
            {
                // Extract the zip to the targeted location
                using ZipArchive archive = new(memStream);

                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = archive.Entries[i];
                    string fileName = Path.Combine(downloadPath, entry.FullName);
                    if (entry.Name == "")
                    {
                        Directory.CreateDirectory(fileName);
                        continue;
                    }

                    float progress = (float)i / archive.Entries.Count;
                    extractBar.Report(progress);
                    try
                    {
                        entry.ExtractToFile(fileName, true);
                    }
                    catch { }
                }
            });
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = downloadPath,
                    FileName = Path.Combine(downloadPath, "Celery.exe")
                }
            }.Start();
            Environment.Exit(0);
        }

    }
}