using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Celery.Core;
using Newtonsoft.Json;

namespace Celery.Services;

public class Asset
{
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("id")]
    public long Id { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("label")]
    public string Label { get; set; }
    [JsonProperty("uploader")]
    public Author Uploader { get; set; }
    [JsonProperty("content_type")]
    public string ContentType { get; set; }
    [JsonProperty("state")]
    public string State { get; set; }
    [JsonProperty("size")]
    public long Size { get; set; }
    [JsonProperty("download_count")]
    public int DownloadCount { get; set; }
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
    [JsonProperty("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }
}

public class Author
{
    [JsonProperty("login")]
    public string Login { get; set; }
    [JsonProperty("id")]
    public long Id { get; set; }
    [JsonProperty("node_id")]
    public string NodeId { get; set; }
    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; }
    [JsonProperty("gravator_id")]
    public string GravatorId { get; set; }
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }
    [JsonProperty("followers_url")]
    public string FollowersUrl { get; set; }
    [JsonProperty("following_url")]
    public string FollowingUrl { get; set; }
    [JsonProperty("gists_url")]
    public string GistsUrl { get; set; }
    [JsonProperty("starred_url")]
    public string StarredUrl { get; set; }
    [JsonProperty("subscriptions_url")]
    public string SubscriptionsUrl { get; set; }
    [JsonProperty("organizations_url")]
    public string OrganizationsUrl { get; set; }
    [JsonProperty("repos_url")]
    public string ReposUrl { get; set; }
    [JsonProperty("events_url")]
    public string EventsUrl { get; set; }
    [JsonProperty("received_events_url")]
    public string ReceivedEventsUrl { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("site_admin")]
    public bool SiteAdmin { get; set; }
}

public class Release
{
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("assets_url")]
    public string AssetsUrl { get; set; }
    [JsonProperty("upload_url")]
    public string UploadUrl { get; set; }
    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }
    [JsonProperty("id")]
    public long Id { get; set; }
    [JsonProperty("author")]
    public Author Author { get; set; }
    [JsonProperty("node_id")]
    public string NodeId { get; set; }
    [JsonProperty("tag_name")]
    public string TagName { get; set; }
    [JsonProperty("target_commitish")]
    public string TargetCommitish { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("draft")]
    public bool Draft { get; set; }
    [JsonProperty("prerelease")]
    public bool Prerelease { get; set; }
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
    [JsonProperty("published_at")]
    public DateTime PublishedAt { get; set; }
    [JsonProperty("assets")]
    public List<Asset> Assets { get; set; }
    [JsonProperty("tarball_url")]
    public string TarballUrl { get; set; }
    [JsonProperty("zipball_url")]
    public string ZipballUrl { get; set; }
    [JsonProperty("body")]
    public string Body { get; set; }
}

public interface IUpdateService
{
    Task CheckUpdate();
}

public class UpdateService : ObservableObject, IUpdateService
{
    private ILoggerService LoggerService { get; }
        
    public UpdateService(ILoggerService loggerService)
    {
        LoggerService = loggerService;
    }

    public async Task CheckUpdate()
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("User-Agent", "Celery");

        // Download information about the latest release from GitHub
        using HttpResponseMessage response = await client.GetAsync(Config.GitHubLatestReleaseUrl);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            string content = await response.Content.ReadAsStringAsync();
            Dictionary<string, string> body = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if (body.TryGetValue("message", out string msg))
            {
                // I can already tell that people are going to be dumb and send a screenshot of the error message.
                Regex regex = new("((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}");
                msg = regex.Replace(msg, "your IP");
                LoggerService.Error($"Couldn't check if there were any updates: {msg}");
            }
            return;
        }
        
        // Parse the response
        string json = await response.Content.ReadAsStringAsync();
        Release release;
        try
        {
            release = JsonConvert.DeserializeObject<Release>(json);
        }
        catch (JsonReaderException)
        {
            LoggerService.Error("Got an invalid response from the server while checking the latest version.");
            return;
        }
        catch (Exception)
        {
            LoggerService.Error("Unknown exception occured while checking new updates.");
            return;
        }
        
        // Parse the latest version so that it can be compared
        if (!Version.TryParse(release.TagName, out Version latestVersion))
        {
            LoggerService.Error($"Couldn't parse '{release.TagName}' as a version");
            return;
        }

        // Check if the latest version is newer than the current version
        if (Config.Version >= latestVersion)
            return;

        // Start the updater
        File.WriteAllBytes(Config.UpdaterPath, Config.CeleryUpdater);
        new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Config.UpdaterPath,
                Arguments = $"\"{release.Assets[0].BrowserDownloadUrl}\" \"{Config.ApplicationPath}\"",
                Verb = "runas"
            }
        }.Start();
        if (App.LspProcess != null && !App.LspProcess.HasExited)
            App.LspProcess.Kill();
        Application.Current.Shutdown();
    }
}