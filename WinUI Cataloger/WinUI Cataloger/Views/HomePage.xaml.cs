using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WinUI_Cataloger.Models;

namespace WinUI_Cataloger.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly ObservableCollection<AppGroup> _groupedApps = new();

        public HomePage()
        {
            InitializeComponent();
            GroupedAppsSource.Source = _groupedApps;
            Loaded += HomePage_Loaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = App.MainWindowInstance;
            if (mainWindow != null)
            {
                CategoryTreeView.ItemsSource = mainWindow.CategoryNodes;
                var flatApps = mainWindow.GetFlatApps();
                TotalAppsCountText.Text = $"{flatApps.Count} apps";
                PopulateGroups(flatApps, "");

                // Listen to load completion in case we loaded async after navigation
                mainWindow.DataLoaded += MainWindow_DataLoaded;
            }
        }

        private void MainWindow_DataLoaded(object? sender, EventArgs e)
        {
            var mainWindow = App.MainWindowInstance;
            if (mainWindow != null)
            {
                CategoryTreeView.ItemsSource = mainWindow.CategoryNodes;
                var flatApps = mainWindow.GetFlatApps();
                TotalAppsCountText.Text = $"{flatApps.Count} apps";
                PopulateGroups(flatApps, SearchBox.Text);
            }
        }

        public void PopulateGroups(List<FlatAppItem> apps, string query)
        {
            string q = query.Trim().ToLower();

            IEnumerable<FlatAppItem> filtered = string.IsNullOrEmpty(q)
                ? apps
                : apps.Where(a =>
                    a.Name.ToLower().Contains(q) ||
                    a.Url.ToLower().Contains(q) ||
                    a.Indicator.ToLower().Contains(q));

            var newGroups = filtered
                .GroupBy(a => a.GroupKey)
                .Select(g =>
                {
                    var first = g.First();
                    var group = new AppGroup(g.Key, first.GroupKey, first.CategoryIconPath);
                    foreach (var item in g) group.Add(item);
                    return group;
                })
                .ToList();

            _groupedApps.Clear();
            foreach (var g in newGroups) _groupedApps.Add(g);

            bool hasResults = _groupedApps.Count > 0;
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = hasResults ? Visibility.Collapsed : Visibility.Visible;
            if (AppsGridView != null && apps.Count > 0)
                AppsGridView.Visibility = hasResults ? Visibility.Visible : Visibility.Collapsed;
            if (LoadingRing != null)
                LoadingRing.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var mainWindow = App.MainWindowInstance;
            if (mainWindow != null)
            {
                PopulateGroups(mainWindow.GetFlatApps(), sender.Text);
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var mainWindow = App.MainWindowInstance;
            if (mainWindow != null)
            {
                PopulateGroups(mainWindow.GetFlatApps(), sender.Text);
            }
        }

        private void CategoryTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is CategoryNode node)
            {
                ScrollToCategory(node.Tag);
                CategoryFlyout.Hide();
            }
        }

        private async void AppCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is FlatAppItem app)
            {
                try { await Windows.System.Launcher.LaunchUriAsync(app.NavigateUri); } catch { }
            }
        }

        private void AppsGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is GridView gridView && gridView.ItemsPanelRoot is ItemsWrapGrid panel)
            {
                // Subtract 18 to account for GridView's right padding (10) and vertical scrollbar (8)
                double availableWidth = e.NewSize.Width - 18;
                if (availableWidth <= 0) return;

                double minWidth = 180;
                int columns = (int)Math.Max(1, Math.Floor(availableWidth / minWidth));

                // Divide the remaining width evenly among all columns
                panel.ItemWidth = Math.Max(minWidth, availableWidth / columns);
            }
        }

        private void ScrollToCategory(string tag)
        {
            try
            {
                string categoryKey = tag.Contains("::") ? tag.Split(new[] { "::" }, StringSplitOptions.None)[0] : tag;
                string? subKey = tag.Contains("::") ? tag.Split(new[] { "::" }, StringSplitOptions.None)[1] : null;

                FlatAppItem? target = null;
                foreach (var group in _groupedApps)
                {
                    foreach (var item in group)
                    {
                        if (item.CategoryKey == categoryKey &&
                            (subKey == null || item.SubGroupLabel == subKey || item.GroupKey.EndsWith(subKey)))
                        {
                            target = item;
                            break;
                        }
                    }
                    if (target != null) break;
                }

                if (target != null)
                {
                    AppsGridView.ScrollIntoView(target, ScrollIntoViewAlignment.Leading);
                }
            }
            catch { }
        }

        private void Card_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                if (element.RenderTransform is not Microsoft.UI.Xaml.Media.TranslateTransform transform)
                {
                    transform = new Microsoft.UI.Xaml.Media.TranslateTransform();
                    element.RenderTransform = transform;
                }

                var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = -4,
                    Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                storyboard.Children.Add(animation);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, transform);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Y");
                storyboard.Begin();
            }
        }

        private void Card_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is UIElement element && element.RenderTransform is Microsoft.UI.Xaml.Media.TranslateTransform transform)
            {
                var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                storyboard.Children.Add(animation);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, transform);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Y");
                storyboard.Begin();
            }
        }
    }
}
