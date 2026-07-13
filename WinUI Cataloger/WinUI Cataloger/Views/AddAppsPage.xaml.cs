using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;

using System.Collections.Generic;
using System.Collections.Specialized;
using WinUI_Cataloger.Models;

namespace WinUI_Cataloger.Views
{
    public sealed partial class AddAppsPage : Page
    {
        private EditPage? _parent;

        public AddAppsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is EditPage parent)
            {
                _parent = parent;
                QueueListView.ItemsSource = _parent.BatchQueue;
                AppForm.InputChanged += AppForm_InputChanged;
                _parent.BatchQueue.CollectionChanged += BatchQueue_CollectionChanged;
                UpdateVisualPreview();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            AppForm.InputChanged -= AppForm_InputChanged;
            if (_parent != null)
            {
                _parent.BatchQueue.CollectionChanged -= BatchQueue_CollectionChanged;
            }
        }

        private void QueueAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parent == null) return;

            if (!AppForm.IsValid)
            {
                _parent.ShowStatus("Please fill in Name, URL, and select a Category.", InfoBarSeverity.Warning);
                return;
            }

            var item = new FlatAppItem
            {
                Name = AppForm.AppName,
                Url = AppForm.AppUrl,
                LogoUrl = AppForm.LogoUrl,
                GroupKey = AppForm.SelectedCategory,
                Indicator = AppForm.DesignIndicator,
                IsFoss = AppForm.IsFoss,
                IsPaid = AppForm.IsPaid,
                IsPlanned = AppForm.IsPlanned,
                IsNew = true // Set the new app indicator flag
            };

            // Check for duplicates in existing README entries (loaded in MainWindow)
            var existingApps = App.MainWindowInstance?.GetFlatApps();
            if (existingApps != null)
            {
                if (existingApps.Any(a => a.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.ShowStatus($"Error: App with the name '{item.Name}' already exists in the catalog.", InfoBarSeverity.Error);
                    return;
                }
                if (existingApps.Any(a => a.Url.Equals(item.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.ShowStatus($"Error: App with the URL '{item.Url}' already exists in the catalog.", InfoBarSeverity.Error);
                    return;
                }
            }

            // Check for duplicates within the current Batch Queue
            if (_parent.BatchQueue.Any(a => a.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _parent.ShowStatus($"Error: App with the name '{item.Name}' is already in the batch queue.", InfoBarSeverity.Error);
                return;
            }
            if (_parent.BatchQueue.Any(a => a.Url.Equals(item.Url, StringComparison.OrdinalIgnoreCase)))
            {
                _parent.ShowStatus($"Error: App with the URL '{item.Url}' is already in the batch queue.", InfoBarSeverity.Error);
                return;
            }

            _parent.BatchQueue.Add(item);
            _parent.ShowStatus($"Added '{item.Name}' to batch queue.", InfoBarSeverity.Informational);
            AppForm.ClearForm();
        }

        private void EditQueueItem_Click(object sender, RoutedEventArgs e)
        {
            if (_parent == null) return;

            if (sender is FrameworkElement el && el.DataContext is FlatAppItem item)
            {
                AppForm.PopulateForm(item);
                _parent.BatchQueue.Remove(item);
                _parent.ShowStatus($"Loaded '{item.Name}' into the form for editing.", InfoBarSeverity.Informational);
            }
        }

        private void RemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            if (_parent == null) return;

            if (sender is FrameworkElement el && el.DataContext is FlatAppItem item)
            {
                _parent.BatchQueue.Remove(item);
                _parent.ShowStatus($"Removed '{item.Name}' from queue.", InfoBarSeverity.Informational);
            }
        }

        private void AppForm_InputChanged(object? sender, EventArgs e)
        {
            ValidateInputsRealTime();
        }

        private void ValidateInputsRealTime()
        {
            if (_parent == null) return;

            string appName = AppForm.AppName;
            string appUrl = AppForm.AppUrl;

            if (string.IsNullOrWhiteSpace(appName) && string.IsNullOrWhiteSpace(appUrl))
            {
                if (_parent.IsStatusErrorOpen)
                {
                    _parent.HideStatus();
                }
                return;
            }

            var existingApps = App.MainWindowInstance?.GetFlatApps();

            if (!string.IsNullOrWhiteSpace(appName))
            {
                if (existingApps != null && existingApps.Any(a => a.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.ShowStatus($"Warning: App name '{appName}' already exists in the catalog.", InfoBarSeverity.Error);
                    return;
                }
                if (_parent.BatchQueue.Any(a => a.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.ShowStatus($"Warning: App name '{appName}' is already in the batch queue.", InfoBarSeverity.Error);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(appUrl))
            {
                if (existingApps != null && existingApps.Any(a => a.Url.Equals(appUrl, StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.ShowStatus($"Warning: App URL '{appUrl}' already exists in the catalog.", InfoBarSeverity.Error);
                    return;
                }
                if (_parent.BatchQueue.Any(a => a.Url.Equals(appUrl, StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.ShowStatus($"Warning: App URL '{appUrl}' is already in the batch queue.", InfoBarSeverity.Error);
                    return;
                }
            }

            if (_parent.IsStatusErrorOpen)
            {
                _parent.HideStatus();
            }
        }

        private void BatchQueue_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateVisualPreview();
        }

        private void UpdateVisualPreview()
        {
            if (_parent == null) return;

            var existingApps = App.MainWindowInstance?.GetFlatApps() ?? new List<FlatAppItem>();
            var queuedApps = _parent.BatchQueue.ToList();

            bool isQueueEmpty = queuedApps.Count == 0;

            // Toggle Queue Empty State
            QueueListView.Visibility = isQueueEmpty ? Visibility.Collapsed : Visibility.Visible;
            QueueEmptyState.Visibility = isQueueEmpty ? Visibility.Visible : Visibility.Collapsed;

            // Toggle Preview Empty State
            AppsGridView.Visibility = isQueueEmpty ? Visibility.Collapsed : Visibility.Visible;
            PreviewEmptyState.Visibility = isQueueEmpty ? Visibility.Visible : Visibility.Collapsed;

            if (isQueueEmpty)
            {
                GroupedAppsSource.Source = null;
                return;
            }

            // Find categories that have at least one queued app
            var targetCategories = queuedApps.Select(q => q.GroupKey).Distinct().ToHashSet();

            var combinedApps = new List<FlatAppItem>();

            // Get all existing apps in target categories
            foreach (var app in existingApps)
            {
                if (targetCategories.Contains(app.GroupKey))
                {
                    combinedApps.Add(app);
                }
            }

            // Get all queued apps (they already have IsNew = true)
            foreach (var app in queuedApps)
            {
                combinedApps.Add(app);
            }

            // Group and sort
            var grouped = combinedApps
                .GroupBy(a => a.GroupKey)
                .Select(g =>
                {
                    var first = g.First();
                    // Sort alphabetically by Name
                    var sortedList = g.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();

                    var group = new AppGroup(g.Key, first.GroupKey, first.CategoryIconPath);
                    foreach (var item in sortedList)
                    {
                        group.Add(item);
                    }
                    return group;
                })
                .OrderBy(g => g.Key)
                .ToList();

            GroupedAppsSource.Source = grouped;
        }

        private void AppsGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is GridView gridView && gridView.ItemsPanelRoot is ItemsWrapGrid panel)
            {
                // Subtract 18 to account for GridView's right padding and vertical scrollbar
                double availableWidth = e.NewSize.Width - 18;
                if (availableWidth <= 0) return;

                double minWidth = 180;
                int columns = (int)Math.Max(1, Math.Floor(availableWidth / minWidth));

                // Divide the remaining width evenly among all columns
                panel.ItemWidth = Math.Max(minWidth, availableWidth / columns);
            }
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
