using System;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Celery.Utils;

namespace Celery.ScriptHub
{
    public partial class ScriptHub : UserControl
    {
        public int Page = 1;
        public int TotalPages = 1;
        public string CurrentSearch;

        public ScriptHub()
        {
            InitializeComponent();
        }

        private Border CreateTag(string tagName)
        {
            return new Border
            {
                Background = (SolidColorBrush)Application.Current.Resources["HighlightBrush"],
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 3, 0),
                Child = new TextBlock
                {
                    Text = tagName,
                    FontSize = 10,
                    Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundBrush"],
                    Margin = new Thickness(3, 0, 3, 0)
                }
            };
        }

        public async void Load(string search, int page)
        {
            string api = $"https://scriptblox.com/api/script/search?q={HttpUtility.JavaScriptStringEncode(search)}&page={page}";
            string response = await App.HttpClient.GetStringAsync(api);
            if (string.IsNullOrEmpty(response))
            {
                await MessageBoxUtils.ShowMessage("Error", "Couldn't obtain scripts from the webserver.", true, MessageBoxButtons.Ok);
                return;
            }
            ResultObject result = response.FromJson<ResultObject>();
            foreach (ScriptObject script in result.result.scripts)
            {
                ScriptHubResult scriptHubResult = new ScriptHubResult();
                string uri = script.game.imageUrl;
                if (!script.game.imageUrl.StartsWith("http"))
                    uri = "https://scriptblox.com" + script.game.imageUrl;
                scriptHubResult.ThumbnailImage.Source = new BitmapImage(new Uri(uri));
                scriptHubResult.Title.Text = script.Title;
                scriptHubResult.Script = script.script;

                if (script.isPatched)
                    scriptHubResult.TagsPanel.Children.Add(CreateTag("Patched"));
                if (script.scriptType == "paid")
                    scriptHubResult.TagsPanel.Children.Add(CreateTag("Paid"));
                if (script.isUniversal)
                    scriptHubResult.TagsPanel.Children.Add(CreateTag("Universal"));
                if (script.verified)
                    scriptHubResult.TagsPanel.Children.Add(CreateTag("Verified"));
                if (script.key)
                    scriptHubResult.TagsPanel.Children.Add(CreateTag("Key System"));

                scriptHubResult.ExecuteButton.Click += (s, e) =>
                {
                    App.Instance.Celery.Execute(scriptHubResult.Script);
                };

                TotalPages = result.result.totalPages;
                ResultsPanel.Children.Add(scriptHubResult);
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (CurrentSearch == "")
                return;

            ScrollViewer scroll = (ScrollViewer)sender;
            if (scroll.ScrollableHeight - e.VerticalOffset <= 200)
            {
                if (Page < TotalPages)
                {
                    Page++;
                    Load(CurrentSearch, Page);
                }
            }
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                ResultsScrollViewer.ScrollToTop();
                ResultsPanel.Children.Clear();
                Page = 1;
                TotalPages = 1;
                CurrentSearch = SearchBox.Text;
                Load(CurrentSearch, Page);
            }
        }

    }


    public class ResultObject
    {
        public ResultContent result { get; set; }
    }

    public class ResultContent
    {
        public int totalPages { get; set; }
        public ScriptObject[] scripts { get; set; }
    }

    public class ScriptObject
    {
        public string _id { get; set; }
        public string Title { get; set; }
        public Game game { get; set; }
        public string slug { get; set; }
        public bool verified { get; set; }
        public bool key { get; set; }
        public int views { get; set; }
        public string scriptType { get; set; }
        public bool isUniversal { get; set; }
        public bool isPatched { get; set; }
        public string visibility { get; set; }
        public int rawCount { get; set; }
        public bool showRawCount { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public int __v { get; set; }
        public string script { get; set; }
        public string[] matched { get; set; }
    }

    public class Game
    {
        public long gameId { get; set; }
        public string name { get; set; }
        public string imageUrl { get; set; }
    }

}
