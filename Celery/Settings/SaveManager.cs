using Celery.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Celery.Settings
{
    public class SaveManager
    {
        public string FileName { get; set; }
        public Dictionary<string, object> SaveFile { get; set; }
        public static SaveManager Instance { get; set; }

        public SaveManager(string fileName)
        {
            FileName = fileName;
            Instance = this;
            SaveFile = new Dictionary<string, object>();
            if (File.Exists(FileName))
            {
                Dictionary<string, object> data = File.ReadAllText(FileName).FromJson<Dictionary<string, object>>();
                if (data != null)
                {
                    foreach (KeyValuePair<string, object> pair in data)
                    {
                        SaveFile[pair.Key] = pair.Value;
                    }
                }
            }
        }

        public void Save(string identifier, object value)
        {
            SaveFile[identifier] = value;
            string json = SaveFile.ToJson();
            File.WriteAllText(FileName, json);
        }

        public T Load<T>(string identifier, T defaultValue)
        {
            if (!File.Exists(FileName))
                return defaultValue;

            string json = File.ReadAllText(FileName);
            Dictionary<string, object> data = json.FromJson<Dictionary<string, object>>();
            if (data == null)
            {
                return defaultValue;
            }
            else
            {
                if (data.ContainsKey(identifier))
                {
                    return (T)Convert.ChangeType(data[identifier], typeof(T));
                }
                else
                {
                    return defaultValue;
                }
            }
        }

    }
}
