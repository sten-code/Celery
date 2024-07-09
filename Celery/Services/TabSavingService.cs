using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Celery.Controls;
using Celery.Core;
using Newtonsoft.Json;

namespace Celery.Services;

public class TabData
{
    [JsonProperty("filename")] public string FileName { get; set; }
    [JsonProperty("content")] public string Content { get; set; }
    [JsonProperty("index")] public int Index { get; set; }
    [JsonProperty("uid")] public string Uid { get; set; }
}

public interface ITabSavingService
{
    Task Save();
    void Load();
}

public class TabSavingService : ObservableObject, ITabSavingService
{
    private TabsHost TabsHost { get; }

    public TabSavingService(TabsHost tabsHost)
    {
        TabsHost = tabsHost;
    }

    public async Task Save()
    {
        List<Tab> tabs = TabsHost.GetTabs();
        string[] uids = tabs.Select(o => o.Uid).ToArray();

        foreach (string file in Directory.GetFiles(Config.TabsPath))
        {
            string uid = Path.GetFileName(file);
            if (!uids.Contains(uid))
                File.Delete(file);
        }

        string[] uidsInFiles = Directory.GetFiles(Config.TabsPath).Select(Path.GetFileName).ToArray();
        for (int i = 0; i < tabs.Count; i++)
        {
            Tab tab = tabs[i];
            if (tab.Content is not Editor editor)
                continue;

            if (tab.Uid == "")
                tab.Uid = Guid.NewGuid().ToString();

            if (uidsInFiles.Contains(tab.Uid))
            {
                if (!editor.CanExecuteJavascriptInMainFrame)
                    continue;

                File.Delete(tab.Uid);
            }

            string text = await editor.GetText();
            TabData data = new()
            {
                FileName = tab.FileName,
                Content = text,
                Index = i,
                Uid = tab.Uid
            };
            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText(Path.Combine(Config.TabsPath, tab.Uid), json);
        }
    }

    public void Load()
    {
        List<TabData> tabs = [];
        foreach (string file in Directory.GetFiles(Config.TabsPath))
        {
            string json = File.ReadAllText(file);
            TabData data = JsonConvert.DeserializeObject<TabData>(json);
            tabs.Add(data);
        }
        tabs = tabs.OrderBy(o => o.Index).ToList();
        foreach (TabData data in tabs)
        {
            string header = "New Tab";
            if (data.FileName != "")
                header = Path.GetFileName(data.FileName);
            Tab tab = TabsHost.MakeTab(data.Content, header);
            tab.FileName = data.FileName;
            tab.Uid = data.Uid;
        }
    }
}