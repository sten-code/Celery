using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;

namespace CeleryUpdater
{
    public class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(int hWnd, string text, string caption, uint type);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void Main(string[] args)
        {
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, 0); // 0 = hide, 5 = show

            if (args.Length != 3)
            {
                MessageBox(0, $"{args.Length} arguments given, 3 are required.", "Error",  0x10);
                return;
            }

            string downloadUrl = args[0];
            string downloadPath = args[1];
            string downloadFileName = downloadPath + "\\files.zip";
            if (!int.TryParse(args[2], out int processId))
            {
                MessageBox(0, $"Process ID: {args[2]} has to be a valid int", "Error", 0x10);
                return;
            }

            try
            {
                Process.GetProcessById(processId).Kill();

                // The directory isn't immediately after accessible after killing the program so just try to delete it until its allowed
                while (Directory.GetDirectories(downloadPath).Length != 0 || Directory.GetFiles(downloadPath).Length != 0)
                {
                    try
                    {
                        foreach (string dir in Directory.GetDirectories(downloadPath)) Directory.Delete(dir, true);
                        foreach (string file in Directory.GetFiles(downloadPath)) File.Delete(file);
                    }
                    catch { }
                }

                new WebClient().DownloadFile(downloadUrl, downloadFileName);
                ZipFile.ExtractToDirectory(downloadFileName, downloadPath);
                File.Delete(downloadFileName);
                Process.Start(downloadPath + "\\Celery.exe");
            }
            catch (Exception ex)
            {
                MessageBox(0, ex.Message.ToString(), "Error", 0x10);
            }
        }

    }
}
