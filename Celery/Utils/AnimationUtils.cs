using System;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace Celery.Utils
{
    public static class AnimationUtils
    {

        public static IEasingFunction EaseInOut { get; } = new QuarticEase
        {
            EasingMode = EasingMode.EaseInOut
        };

        public static IEasingFunction EaseIn { get; } = new QuarticEase
        {
            EasingMode = EasingMode.EaseIn
        };

        public static IEasingFunction EaseOut { get; } = new QuarticEase
        {
            EasingMode = EasingMode.EaseOut
        };

        public static void AnimateMargin(DependencyObject obj, Thickness get, Thickness set, IEasingFunction easing, int duration = 500)
        {
            AnimateThicknessProperty(obj, get, set, FrameworkElement.MarginProperty, easing, duration);
        }

        public static void AnimateWidth(DependencyObject obj, double get, double set, IEasingFunction easing, int duration = 500)
        {
            AnimateDoubleProperty(obj, get, set, FrameworkElement.WidthProperty, easing, duration);
        }

        public static void AnimateHeight(DependencyObject obj, double get, double set, IEasingFunction easing, int duration = 500)
        {
            AnimateDoubleProperty(obj, get, set, FrameworkElement.HeightProperty, easing, duration);
        }

        public static void AnimateDoubleProperty(DependencyObject obj, double get, double set, DependencyProperty property, IEasingFunction easing, int duration = 500)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                From = get,
                To = set,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(anim, obj);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            Storyboard sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Begin();
            sb.Children.Remove(anim);
        }

        public static void AnimateDoubleProperty(DependencyObject obj, double get, double set, string property, IEasingFunction easing, int duration = 500)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                From = get,
                To = set,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(anim, obj);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            Storyboard sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Begin();
            sb.Children.Remove(anim);
        }

        public static void AnimateThicknessProperty(DependencyObject obj, Thickness get, Thickness set, DependencyProperty property, IEasingFunction easing, int duration = 500)
        {
            ThicknessAnimation anim = new ThicknessAnimation
            {
                From = get,
                To = set,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(anim, obj);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            Storyboard sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Begin();
            sb.Children.Remove(anim);
        }

        public static void AnimateColorProperty(DependencyObject obj, Color get, Color set, DependencyProperty property, IEasingFunction easing, int duration = 500)
        {
            ColorAnimation anim = new ColorAnimation
            {
                From = get,
                To = set,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(anim, obj);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            Storyboard sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Begin();
            sb.Children.Remove(anim);
        }

    }
}
