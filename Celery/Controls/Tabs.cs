using System.Threading.Tasks;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using Celery.Utils;
using System.Windows;

namespace Celery.Controls
{
    public class Tabs : TabControl
    {
        #region Properties

        public static readonly DependencyProperty TabWidthProperty = DependencyProperty.Register(nameof(TabWidth), typeof(double), typeof(Tabs), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty TabSpacingProperty = DependencyProperty.Register(nameof(TabSpacing), typeof(double), typeof(Tabs), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty MaxWidthSubtractionProperty = DependencyProperty.Register(nameof(MaxWidthSubtraction), typeof(double), typeof(Tabs), new PropertyMetadata(default(double)));

        #endregion

        public double TabWidth { get; set; } = 100;
        public double TabSpacing { get; set; } = 4;
        public double MaxWidthSubtraction { get; set; } = 5;
        public Func<string, UIElement> GetContent { get; set; }

        private double addTabButtonWidth = 23;

        private T GetTemplateItem<T>(Control elem, string name)
        {
            return elem.Template.FindName(name, elem) is T name1 ? name1 : default;
        }

        public Tabs()
        {
            Loaded += (s, e) =>
            {
                GetTemplateItem<Button>(this, "AddTabButton").Click += (t, a) =>
                {
                    MakeTab();
                };
            };
        }

        public TabItem MakeTab(string text = "", string title = "New Tab")
        {
            TabItem tab = new TabItem
            {
                Header = title
            };

            if (GetContent != null)
                tab.Content = GetContent(text);

            tab.MouseDown += (s, e) =>
            {
                if (e.OriginalSource is Border || e.OriginalSource is TextBlock)
                {
                    if (e.MiddleButton == MouseButtonState.Pressed)
                    {
                        CloseTab(tab);
                    }
                }
            };
            tab.Loaded += (s, e) =>
            {
                GetTemplateItem<Button>(tab, "CloseButton").Click += (c, a) =>
                {
                    CloseTab(tab);
                };
            };

            double maxWidth = ActualWidth - MaxWidthSubtraction;
            double width = addTabButtonWidth + (TabSpacing * 2) + TabWidth;
            foreach (TabItem t in Items)
            {
                width += t.ActualWidth;
            }
            double newWidth = TabWidth;
            if (width > maxWidth)
            {
                newWidth = (maxWidth - addTabButtonWidth) / (Items.Count + 1);
                foreach (TabItem t in Items)
                {
                    AnimationUtils.AnimateWidth(t, t.ActualWidth, newWidth, AnimationUtils.EaseInOut, 200);
                }
            }

            SelectedIndex = Items.Add(tab);
            AnimationUtils.AnimateWidth(tab, 0, newWidth, AnimationUtils.EaseInOut, 200);
            return tab;
        }

        public async void CloseTab(TabItem tab)
        {
            AnimationUtils.AnimateWidth(tab, tab.ActualWidth, 0, AnimationUtils.EaseInOut, 200);
            double maxWidth = ActualWidth - MaxWidthSubtraction;
            double width = -(TabWidth + TabSpacing);
            foreach (TabItem t in Items)
            {
                width += t.ActualWidth;
            }

            if (width < maxWidth)
            {
                double newWidth = Math.Min((maxWidth - addTabButtonWidth) / (Items.Count - 1), TabWidth);
                foreach (TabItem t in Items)
                {
                    if (t != tab)
                        AnimationUtils.AnimateWidth(t, t.ActualWidth, newWidth, AnimationUtils.EaseInOut, 200);
                }
            }

            await Task.Delay(200);
            if (tab.Content is IDisposable)
                ((IDisposable)tab.Content).Dispose();
            Items.Remove(tab);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            double maxWidth = ActualWidth - MaxWidthSubtraction;
            double width = 0;
            foreach (TabItem t in Items)
            {
                width += t.ActualWidth;
            }

            double newWidth = Math.Min((maxWidth - addTabButtonWidth) / Items.Count, TabWidth);
            foreach (TabItem t in Items)
            {
                AnimationUtils.AnimateWidth(t, t.ActualWidth, newWidth, AnimationUtils.EaseInOut, 0); // For some reason setting the width doesn't work so an animation with a duration of 0ms will have to do.
            }
        }

    }
}
