using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WinUI_Cataloger.Models;
using WinUI_Cataloger.Views;

namespace WinUI_Cataloger
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string? _readmePath;
        public ObservableCollection<AppItem> RecentApps { get; } = new();
        public ObservableCollection<CategoryNode> CategoryNodes { get; } = new();
        private readonly List<AppCategory> _allCategories = new();

        private List<FlatAppItem> _allFlatApps = new();

        public event EventHandler? DataLoaded;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class JsonApp
        {
            public string name { get; set; } = "";
            public string link { get; set; } = "";
            public string tag { get; set; } = "";
            public string price { get; set; } = "";
            public string? logo_url { get; set; }
        }

        public class JsonSubgroup
        {
            public string subheading { get; set; } = "";
            public List<JsonApp> apps { get; set; } = new();
        }

        public class JsonCategory
        {
            public string heading { get; set; } = "";
            public List<JsonSubgroup> subgroups { get; set; } = new();
        }

        public class CategoryMetadataItem
        {
            public string name { get; set; } = "";
            public string icon { get; set; } = "";
        }

        public class CategoryMetadataRoot
        {
            public Dictionary<string, CategoryMetadataItem> categories { get; set; } = new();
        }

        public MainWindow()
        {
            InitializeComponent();
            RootGrid.DataContext = this;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            ConfigureWindow();

            _readmePath = FindReadmePath();

            // Set Homepage as default
            MainNav.SelectedItem = HomeItem;
            NavigateTo("Home");

            _ = LoadAllCategoriesAndAppsAsync();
        }

        public List<FlatAppItem> GetFlatApps() => _allFlatApps;

        private void ConfigureWindow()
        {
            try
            {
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "WindowIcon.ico");
                if (File.Exists(iconPath)) appWindow.SetIcon(iconPath);
                
                // Maximize window by default
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
                {
                    overlappedPresenter.Maximize();
                }
            }
            catch { }
        }

        public string? FindReadmePath()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                string readmePath = Path.Combine(dir, "README.md");
                if (File.Exists(readmePath)) return readmePath;
                dir = Path.GetDirectoryName(dir) ?? "";
            }
            return null;
        }

        public async System.Threading.Tasks.Task LoadAllCategoriesAndAppsAsync()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "data", "apps-data.json");
            if (!File.Exists(dbPath)) return;

            var (cats, recent, flat, nodes) = await System.Threading.Tasks.Task.Run(async () =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                List<JsonCategory>? jsonCategories = null;
                using (var stream = File.OpenRead(dbPath))
                {
                    jsonCategories = await JsonSerializer.DeserializeAsync<List<JsonCategory>>(stream, options);
                }

                CategoryMetadataRoot? metadataRoot = null;
                string metaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "data", "category-metadata.json");
                if (File.Exists(metaPath))
                {
                    try 
                    { 
                        using (var stream = File.OpenRead(metaPath))
                        {
                            metadataRoot = await JsonSerializer.DeserializeAsync<CategoryMetadataRoot>(stream, options); 
                        }
                    } 
                    catch { }
                }

                var resultCats = new List<AppCategory>();
                var resultRecent = new List<AppItem>();
                var resultFlat = new List<FlatAppItem>();
                var seenUrls = new HashSet<string>();

                if (jsonCategories != null)
                {
                    foreach (var jCat in jsonCategories)
                    {
                        string categoryKey = jCat.heading;
                        string displayName = categoryKey;
                        string iconPath = "";

                        if (metadataRoot?.categories != null && metadataRoot.categories.TryGetValue(categoryKey, out var metaItem))
                        {
                            displayName = metaItem.name;
                            iconPath = $"ms-appx:///Assets/category/{metaItem.icon}";
                        }

                        var category = new AppCategory { Name = categoryKey, DisplayName = displayName, IconPath = iconPath };

                        foreach (var jSub in jCat.subgroups)
                        {
                            if (string.IsNullOrEmpty(jSub.subheading))
                            {
                                foreach (var jApp in jSub.apps)
                                {
                                    var app = MapJsonAppToFlatItem(jApp, categoryKey, displayName, displayName, iconPath, "");
                                    category.Apps.Add(app);
                                    if (seenUrls.Add(app.Url)) resultFlat.Add(app);
                                    if (category.Name.Contains("Newly Added Apps")) resultRecent.Add(app);
                                }
                            }
                            else
                            {
                                string subDisplayName = jSub.subheading;
                                if (metadataRoot?.categories != null && metadataRoot.categories.TryGetValue(jSub.subheading, out var subMeta))
                                    subDisplayName = subMeta.name;

                                string groupKey = $"{displayName} › {subDisplayName}";
                                var subCategory = new AppSubCategory { Name = subDisplayName };
                                foreach (var jApp in jSub.apps)
                                {
                                    var app = MapJsonAppToFlatItem(jApp, categoryKey, groupKey, displayName, iconPath, subDisplayName);
                                    subCategory.Apps.Add(app);
                                    if (seenUrls.Add(app.Url)) resultFlat.Add(app);
                                }
                                category.SubCategories.Add(subCategory);
                            }
                        }

                        resultCats.Add(category);
                    }
                }

                var resultNodes = new List<CategoryNode>();
                foreach (var cat in resultCats)
                {
                    var catNode = new CategoryNode { Name = cat.DisplayName, Tag = cat.Name, IconPath = cat.IconPath };
                    foreach (var sub in cat.SubCategories)
                        catNode.Children.Add(new CategoryNode { Name = sub.Name, Tag = $"{cat.Name}::{sub.Name}" });
                    resultNodes.Add(catNode);
                }

                return (resultCats, resultRecent, resultFlat, resultNodes);
            });

            _allCategories.Clear();
            _allFlatApps.Clear();
            RecentApps.Clear();
            CategoryNodes.Clear();

            foreach (var c in cats) _allCategories.Add(c);
            foreach (var a in recent) RecentApps.Add(a);
            _allFlatApps.AddRange(flat);
            foreach (var n in nodes) CategoryNodes.Add(n);

            DataLoaded?.Invoke(this, EventArgs.Empty);

            // Force GC cleanup after startup loading is finished
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private static FlatAppItem MapJsonAppToFlatItem(JsonApp jApp, string categoryKey, string groupKey,
            string categoryDisplayName, string iconPath, string subGroupLabel)
        {
            return new FlatAppItem
            {
                Name = jApp.name,
                Url = jApp.link,
                Indicator = string.IsNullOrEmpty(jApp.tag) ? "WD" : jApp.tag,
                LogoUrl = jApp.logo_url ?? "",
                IsFoss = jApp.price == "FOSS",
                IsPaid = jApp.price == "Paid",
                IsPlanned = jApp.price == "Planned",
                CategoryKey = categoryKey,
                GroupKey = groupKey,
                CategoryIconPath = iconPath,
                SubGroupLabel = subGroupLabel,
            };
        }

        private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                string tag = args.SelectedItemContainer.Tag.ToString() ?? "Home";
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string tag)
        {
            if (tag == "Home")
            {
                ContentFrame.Navigate(typeof(HomePage));
            }
            else if (tag == "Edit")
            {
                ContentFrame.Navigate(typeof(EditPage));
            }
        }
    }
}
