using Celery.Utils;
using System.Threading.Tasks;
using System.Windows;

namespace Celery
{
    public partial class Startup : Window
    {
        public Startup()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thickness welcomeEnd = WelcomeLabel.Margin;
            WelcomeLabel.Margin = new Thickness(0, 80, -200, 0);
            Thickness celeryEnd = CeleryLabel.Margin;
            CeleryLabel.Margin = new Thickness(0, 120, -200, 0);
            Thickness celeryLogoEnd = CeleryLogo.Margin;
            CeleryLogo.Margin = new Thickness(-200,0, 0, 0);

            // Starting
            AnimationUtils.AnimateDoubleProperty(this, 0, 1, OpacityProperty, AnimationUtils.EaseInOut);
            await Task.Delay(500);

            AnimationUtils.AnimateMargin(WelcomeLabel, WelcomeLabel.Margin, welcomeEnd, AnimationUtils.EaseOut, 700);
            await Task.Delay(100);
            AnimationUtils.AnimateMargin(CeleryLabel, CeleryLabel.Margin, celeryEnd, AnimationUtils.EaseOut, 700);
            await Task.Delay(100);
            AnimationUtils.AnimateMargin(CeleryLogo, CeleryLogo.Margin, celeryLogoEnd, AnimationUtils.EaseOut, 700);

            await Task.Delay(1500);

            // Closing
            AnimationUtils.AnimateMargin(WelcomeLabel, WelcomeLabel.Margin, new Thickness(0, 80, -200, 0), AnimationUtils.EaseIn, 700);
            await Task.Delay(100);
            AnimationUtils.AnimateMargin(CeleryLabel, CeleryLabel.Margin, new Thickness(0, 120, -200, 0), AnimationUtils.EaseIn, 700);
            await Task.Delay(100);
            AnimationUtils.AnimateMargin(CeleryLogo, CeleryLogo.Margin, new Thickness(-200, 0, 0, 0), AnimationUtils.EaseIn, 700);


            await Task.Delay(500);
            AnimationUtils.AnimateDoubleProperty(this, 1, 0, OpacityProperty, AnimationUtils.EaseInOut);
            await Task.Delay(500);
            Hide();
        }
    }
}
