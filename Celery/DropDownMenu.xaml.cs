using Celery.Controls;
using Celery.Settings;
using Celery.Utils;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Celery
{
    public partial class DropDownMenu : UserControl
    {
        private Border border;
        private UIElement toDisable;
        private bool closing = false;

        private T GetTemplateItem<T>(Control elem, string name)
        {
            return elem.Template.FindName(name, elem) is T name1 ? name1 : default;
        }

        public DropDownMenu(Border border, UIElement toDisable)
        {
            InitializeComponent();
            closing = false;
            this.border = border;
            this.toDisable = toDisable;
        }

        public async void LoadAnnoucements()
        {
            foreach (AnnouncementBox announcement in App.Anouncements)
            {
                if (closing)
                    return;
                UpdateList.Children.Add(announcement);
                AnimationUtils.AnimateMargin(announcement, new Thickness(10, UpdateList.ActualHeight, 10, 0), new Thickness(0, 0, 0, 10), AnimationUtils.EaseOut);
                await Task.Delay(500);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (TabItem tab in ModuleTabs.Items)
            {
                GetTemplateItem<Button>(tab, "CloseButton").Visibility = Visibility.Hidden;
            }
            GetTemplateItem<Button>(ModuleTabs, "AddTabButton").Visibility = Visibility.Hidden;

            /*
            bool aimbotEnabled = false;
            AimbotSettingsMenu.AddSetting(new BooleanSetting("Enabled", "aimbotenabled", false, (value) =>
            {
                aimbotEnabled = value;
                if (value)
                {
                    App.Instance.Celery.Execute(@"
if Aimbot == nil then
    loadstring(game:HttpGet(""https://gist.githubusercontent.com/ao-0/8be8977c323a51c96638fec5dda99557/raw/4327d1c5da6dc8672795be836b361e543d8e1459/celery.wip""))()
end
Aimbot.Enabled = " + (value ? "true" : "false"), false);
                }
            }));

            AimbotSettingsMenu.AddSettings(
                new NumberSetting("FOV", "fov", 100, 50, 500, 5, (value) =>
                {
                    if (aimbotEnabled)
                        App.Instance.Celery.Execute("Aimbot.FOVSize = " + value + "\nprint(\"fov\")", false);

                }),
                new BooleanSetting("Show FOV", "aimbotshowfov", false, (value) =>
                {
                    if (aimbotEnabled)
                        App.Instance.Celery.Execute("Aimbot.ShowFov = " + (value ? "true" : "false") + "\nprint(\"showfov\")", false);
                }),
                new NumberSetting("Smoothness", "aimbotsmoothness", 5, 0, 10, 1, (value) =>
                {
                    if (aimbotEnabled)
                        App.Instance.Celery.Execute("Aimbot.Smoothness = " + value + "\nprint(\"smoothness\")", false);
                }),
                new BooleanSetting("Smoothing Enabled", "aimbotsmoothenabled", false, (value) =>
                {
                    if (aimbotEnabled)
                        App.Instance.Celery.Execute("Aimbot.Smoothing = " + (value ? "true" : "false") + "\nprint(\"smoothing\")", false);
                }),
                new BooleanSetting("Show Prediction", "aimbotprediction", false, (value) =>
                {
                    if (aimbotEnabled)
                        App.Instance.Celery.Execute("Aimbot.Prediction = " + (value ? "true" : "false") + "\nprint(\"prediction\")", false);
                })
            );

            bool espEnabled = false;
            ESPSettingsMenu.AddSetting(new BooleanSetting("Enabled", "espenabled", false, (value) =>
            {
                espEnabled = value;
                if (value)
                {
                    App.Instance.Celery.Execute(@"
if Esp == nil then
    loadstring(game:HttpGet(""https://gist.githubusercontent.com/ao-0/8be8977c323a51c96638fec5dda99557/raw/4327d1c5da6dc8672795be836b361e543d8e1459/celery.wip""))()
end
Esp.Enabled = " + (value ? "true" : "false"), false);
                }
            }));


            ESPSettingsMenu.AddSettings(
                new BooleanSetting("Show Boxes", "espboxes", false, (value) =>
                {
                    if (espEnabled)
                        App.Instance.Celery.Execute("Esp.Boxes = " + value + "\nprint(\"boxes\")", false);
                }),
                new BooleanSetting("Show Tracers", "esptracers", false, (value) =>
                {
                    if (espEnabled)
                        App.Instance.Celery.Execute("Esp.Tracers = " + value + "\nprint(\"tracers\")", false);
                }),
                new BooleanSetting("Show Names", "espnames", false, (value) =>
                {
                    if (espEnabled)
                        App.Instance.Celery.Execute("Esp.Names = " + value + "\nprint(\"names\")", false);
                }),
                new BooleanSetting("Show Team Color", "espteamcolor", false, (value) =>
                {
                    if (espEnabled)
                        App.Instance.Celery.Execute("Esp.TeamColor = " + value + "\nprint(\"teamcolor\")", false);
                })
            );*/

            AnimationUtils.AnimateMargin(ScriptPanel, new Thickness(ScriptPanel.Margin.Left, -ScriptPanel.ActualHeight - 100, ScriptPanel.Margin.Right, ScriptPanel.ActualHeight + 100), ScriptPanel.Margin, AnimationUtils.EaseOut);
            AnimationUtils.AnimateMargin(CreditsPanel, new Thickness(-CreditsPanel.ActualWidth - 100, CreditsPanel.Margin.Top, CreditsPanel.ActualWidth + 100, CreditsPanel.Margin.Bottom), CreditsPanel.Margin, AnimationUtils.EaseOut);
            LoadAnnoucements();
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            closing = true;
            AnimationUtils.AnimateMargin(ScriptPanel, ScriptPanel.Margin, new Thickness(ScriptPanel.Margin.Left, -ScriptPanel.ActualHeight - 100, ScriptPanel.Margin.Right, ScriptPanel.ActualHeight + 100), AnimationUtils.EaseOut);
            AnimationUtils.AnimateMargin(CreditsPanel, CreditsPanel.Margin, new Thickness(-CreditsPanel.ActualWidth - 100, CreditsPanel.Margin.Top, CreditsPanel.ActualWidth + 100, CreditsPanel.Margin.Bottom), AnimationUtils.EaseOut);
            CloseButton.Visibility = Visibility.Hidden;
            App.Instance.ResetBlur(border, toDisable, this);

            double height = 0;
            foreach (AnnouncementBox box in UpdateList.Children)
            {
                if (height >= UpdateListScrollViewer.VerticalOffset - (box.ActualHeight + 10) && height < UpdateListScrollViewer.VerticalOffset + UpdateListScrollViewer.ActualHeight)
                {
                    AnimationUtils.AnimateMargin(box, box.Margin, new Thickness(box.ActualWidth + 100, box.Margin.Top, -box.ActualWidth - 100, box.Margin.Bottom), AnimationUtils.EaseOut);
                }
                height += box.ActualHeight + 10;
            }
            await Task.Delay(500);
            UpdateList.Children.Clear();
        }

        #region Links

        private void JayGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/TheSeaweedMonster");
        }

        private void StenGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/sten-code");
        }

        private void DottikGithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SecondNewtonLaw");
        }

        private void StiizzyCatGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/StiizzyCat");
        }

        private void XibaGithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/ao-0");
        }

        #endregion

    }
}
