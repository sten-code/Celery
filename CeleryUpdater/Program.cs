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
        public static readonly string TempPath = Path.Combine(Path.GetTempPath(), "CeleryDownload");

        public async static Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"{args.Length} arguments given, 2 were expected.");
                return;
            }

            string downloadUrl = args[0];
            string downloadPath = args[1];
            string[] whitelisted =
            [
                "scripts", "autoexec"
            ];

            Console.WriteLine("Downloading...");
            using HttpClient client = new();
            using MemoryStream memStream = new(8000000); // 8MB buffer
            using (ProgressBar downloadBar = new()
                   {
                       NumberOfBlocks = 18,
                       StartBracket = "[",
                       EndBracket = "]",
                       CompletedBlock = "\u2501",
                       IncompleteBlock = "-",
                       AnimationSequence = ".oO\u00b0Oo."
                   })
            {
                await client.DownloadAsync(downloadUrl, memStream, downloadBar);
                memStream.Position = 0;
            }
            Console.WriteLine();
            Console.WriteLine($"Extracting to {TempPath}");
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);

            using (ProgressBar extractBar = new()
                   {
                       NumberOfBlocks = 18,
                       StartBracket = "[",
                       EndBracket = "]",
                       CompletedBlock = "\u2501",
                       IncompleteBlock = "-",
                       AnimationSequence = ".oO\u00b0Oo."
                   })
            {
                await Task.Run(() =>
                {
                    // Extract the zip to the targeted location
                    using ZipArchive archive = new(memStream);

                    for (int i = 0; i < archive.Entries.Count; i++)
                    {
                        ZipArchiveEntry entry = archive.Entries[i];
                        string fileName = Path.Combine(TempPath, entry.FullName);
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
            }

            Console.WriteLine();
            Console.WriteLine("Clearing old installation...");
            if (!Directory.Exists(downloadPath))
                Directory.CreateDirectory(downloadPath);

            bool errorHappened = false;

            foreach (string dir in Directory.GetDirectories(downloadPath))
            {
                string name = Path.GetFileName(dir);
                if (whitelisted.Contains(name))
                    continue;

                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't delete directory '{dir}': " + ex.Message);
                    errorHappened = true;
                }
            }
            foreach (string file in Directory.GetFiles(downloadPath))
            {
                string name = Path.GetFileName(file);
                if (whitelisted.Contains(name))
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't delete file '{file}': " + ex.Message);
                    errorHappened = true;
                }
            }
            
            Console.WriteLine("Moving files...");

            // Move all files
            foreach (string file in Directory.GetFiles(TempPath))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(downloadPath, fileName);
                try
                {
                    File.Move(file, destFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't move file '{file}': " + ex.Message);
                    errorHappened = true;
                }
            }

            // Move all directories
            foreach (string dir in Directory.GetDirectories(TempPath))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(downloadPath, dirName);
                try
                {
                    Directory.Move(dir, destDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't move directory '{dir}': " + ex.Message);
                    errorHappened = true;
                }
            }

            if (!errorHappened)
            {
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
            else
            {
                Console.WriteLine("An error occured");
                Console.ReadKey();
            }
        }

    }
}