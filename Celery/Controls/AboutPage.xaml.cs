using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Celery.Controls
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/celery");
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/sten-code/Celery");
        }

        private void TutorialButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/sten-code/Celery#themetutorial");
        }

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
    }
}
