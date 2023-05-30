using System.IO;
using System.Windows;

namespace Celery.Utils
{
    public class FileUtils
    {
        // Creates the file at the path if it does not exist.
        // Otherwise, returns false if the file does exist.
        public static bool CheckCreateFile(string path)
        {
            if (File.Exists(path))
                return true;

            try
            {
                using (FileStream s = File.Create(path))
                {
                    s.Close();
                    return true;
                }
            }
            catch
            {
                MessageBox.Show("There was an issue while trying to create file `" + path + "`...Please close Celery and run as an administrator", "", MessageBoxButton.OK);
            }
            return false;

        }

        public static bool CreateFileText(string filepath, string content)
        {
            try
            {
                try
                {
                    File.CreateText(filepath);
                }
                catch
                {
                    MessageBox.Show("There was an issue while trying to create file `" + filepath + "`...Please close Celery and run as an administrator", "", MessageBoxButton.OK);
                }

                try
                {
                    File.WriteAllText(filepath, content);
                    return true;
                }
                catch
                { }
            }
            catch
            { }

            return false;
        }

        // Creates the file at the path if it does not exist, and writes information to the file.
        public static bool CheckCreateFile(string path, string defaultValue)
        {
            if (File.Exists(path))
                return true;

            CheckCreateFile(path);

            try
            {
                File.WriteAllText(path, defaultValue);
                return true;
            }
            catch
            {
                MessageBox.Show("There was an issue while trying to write to a file...Please close Celery and run as an administrator", "", MessageBoxButton.OK);
                return false;
            }
        }
    }
}