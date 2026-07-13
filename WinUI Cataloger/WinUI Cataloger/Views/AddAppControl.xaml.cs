using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using CommunityToolkit.WinUI.Controls;
using WinUI_Cataloger.Models;

namespace WinUI_Cataloger.Views
{
    public sealed partial class AddAppControl : UserControl
    {
        private readonly HttpClient _httpClient;
        private string _suggestedLogoUrl = "";
        private string _selectedCategory = "";

        public event EventHandler? InputChanged;

        public AddAppControl()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            UpdatePreview();
            Loaded += (s, e) => LoadCategoryTree();
        }

        private void LoadCategoryTree()
        {
            var mainWindow = App.MainWindowInstance;
            if (mainWindow != null)
            {
                var filtered = System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Where(mainWindow.CategoryNodes,
                        n => !n.Tag.Contains("Newly Added Apps") && !n.Name.Contains("Newly Added Apps"))
                );
                CategoryTree.ItemsSource = filtered;
            }
        }

        public string AppUrl => UrlInput.Text.Trim();
        public string AppName => NameInput.Text.Trim();
        public string LogoUrl => LogoUrlInput.Text.Trim();
        public string SelectedCategory => _selectedCategory;

        public string DesignIndicator
        {
            get
            {
                if (IndicatorSegment.SelectedItem is SegmentedItem item)
                    return item.Tag?.ToString() ?? "WDM";
                return "WDM";
            }
        }

        public bool IsFoss => PricingSegment.SelectedItem is SegmentedItem item && item.Tag?.ToString() == "FOSS";
        public bool IsPaid => PricingSegment.SelectedItem is SegmentedItem item && item.Tag?.ToString() == "Paid";
        public bool IsPlanned => ProjectStateSegment.SelectedItem is SegmentedItem item && item.Tag?.ToString() == "Planned";
        public bool IsDiscontinued => ProjectStateSegment.SelectedItem is SegmentedItem item && item.Tag?.ToString() == "Discontinued";

        public bool IsValid => !string.IsNullOrWhiteSpace(AppUrl) &&
                               !string.IsNullOrWhiteSpace(AppName) &&
                               !string.IsNullOrWhiteSpace(_selectedCategory);

        // ── Category TreeView ────────────────────────────────────────────

        private void CategoryTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is CategoryNode node && node.Children.Count == 0)
            {
                // Leaf node: selectable category. Find parent node
                string parentName = "";
                if (App.MainWindowInstance != null)
                {
                    foreach (var parent in App.MainWindowInstance.CategoryNodes)
                    {
                        if (parent.Children.Contains(node))
                        {
                            parentName = parent.Name;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(parentName))
                {
                    _selectedCategory = $"{parentName} › {node.Name}";
                }
                else
                {
                    _selectedCategory = node.Name;
                }

                CategoryButtonText.Text = _selectedCategory;
                CategoryFlyout.Hide();
                UpdatePreview();
                InputChanged?.Invoke(this, EventArgs.Empty);
            }
            // Parent nodes: let them expand/collapse naturally, no selection
        }

        // ── Text / Input events ──────────────────────────────────────────

        private void UrlInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string url = UrlInput.Text.Trim();
            if (url.Contains("github.com"))
            {
                if (PricingSegment != null) PricingSegment.SelectedIndex = 1; // FOSS
            }
            else if (url.Contains("apps.microsoft.com"))
            {
                if (PricingSegment != null && PricingSegment.SelectedIndex == 1) // if currently FOSS, reset to Free
                {
                    PricingSegment.SelectedIndex = 0; // Free
                }
            }
            Input_TextChanged(sender, e);
        }

        private void Input_TextChanged(object sender, object e)
        {
            UpdatePreview();
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Segmented selection handlers ──────────────────────────────────

        private void PricingSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Input_TextChanged(sender, e);
        }

        private void ProjectStateSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Input_TextChanged(sender, e);
        }

        // ── Logo URL suggestion ──────────────────────────────────────────

        private void LogoUrlInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckLogoUrlSuggestion(LogoUrlInput.Text.Trim());
            Input_TextChanged(sender, e);
        }

        private void CheckLogoUrlSuggestion(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                LogoSuggestionLink.Visibility = Visibility.Collapsed;
                return;
            }

            var match = Regex.Match(input, @"https?://(?:www\.)?github\.com/([^/]+)/([^/]+)/(?:raw|blob)/([^/]+)/([^?#\s]+)");
            if (match.Success)
            {
                string owner = match.Groups[1].Value;
                string repo = match.Groups[2].Value;
                string branch = match.Groups[3].Value;
                string path = match.Groups[4].Value;
                string suggestion = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}";

                if (suggestion != input)
                {
                    LogoSuggestionText.Text = $"Suggest raw URL: {suggestion}";
                    LogoSuggestionLink.Visibility = Visibility.Visible;
                    _suggestedLogoUrl = suggestion;
                    return;
                }
            }

            LogoSuggestionLink.Visibility = Visibility.Collapsed;
        }

        private void LogoSuggestionLink_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_suggestedLogoUrl))
            {
                LogoUrlInput.Text = _suggestedLogoUrl;
                LogoSuggestionLink.Visibility = Visibility.Collapsed;
            }
        }

        // ── Markdown generation ──────────────────────────────────────────

        public string GenerateMarkdownLine()
        {
            string name = string.IsNullOrWhiteSpace(AppName) ? "AppName" : AppName;
            string url = AppUrl;
            string logoUrl = LogoUrl;

            string fossBadge = IsFoss ? " <sup>`FOSS`</sup>" : "";
            string paidBadge = IsPaid ? " `💰`" : "";
            string plannedBadge = IsPlanned ? " `📆 Planned`" : "";
            string discontinuedBadge = IsDiscontinued ? " `❎ Discontinued`" : "";
            string logoComment = !string.IsNullOrWhiteSpace(logoUrl) ? $" <!-- logo: {logoUrl} -->" : "";

            return $"- `{DesignIndicator}` [{name}]({url}){paidBadge}{plannedBadge}{fossBadge}{discontinuedBadge}{logoComment}";
        }

        private void UpdatePreview()
        {
            if (PreviewText != null)
                PreviewText.Text = GenerateMarkdownLine();
        }

        // ── Fetch metadata ───────────────────────────────────────────────

        private async void FetchButton_Click(object sender, RoutedEventArgs e)
        {
            string url = AppUrl;
            if (string.IsNullOrEmpty(url))
            {
                ShowStatus("Please enter a valid URL.", InfoBarSeverity.Warning);
                return;
            }

            FetchButton.IsEnabled = false;

            try
            {
                string html = await _httpClient.GetStringAsync(url);

                if (url.Contains("github.com"))
                    ParseGitHubMetadata(html, url);
                else if (url.Contains("apps.microsoft.com"))
                    ParseMicrosoftStoreMetadata(html);
                else
                    ShowStatus("Fetch metadata supports GitHub or Microsoft Store URLs.", InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to fetch metadata: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                FetchButton.IsEnabled = true;
                UpdatePreview();
                InputChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            if (severity == InfoBarSeverity.Success)
            {
                StatusInfo.IsOpen = false;
                FetchSuccessIcon.Visibility = Visibility.Visible;
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                System.Threading.Tasks.Task.Delay(2500).ContinueWith(t =>
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        FetchSuccessIcon.Visibility = Visibility.Collapsed;
                    });
                });
            }
            else
            {
                StatusInfo.Message = message;
                StatusInfo.Severity = severity;
                StatusInfo.IsOpen = true;
            }
        }

        private void ParseGitHubMetadata(string html, string url)
        {
            string name = "";
            var titleMatch = Regex.Match(html, @"<title>[^<]+</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                string titleText = Regex.Replace(titleMatch.Value, @"<[^>]+>", "").Trim();
                titleText = titleText.Replace("GitHub - ", "");
                int colonIdx = titleText.IndexOf(':');
                if (colonIdx > 0) titleText = titleText.Substring(0, colonIdx);
                string[] parts = titleText.Split('/');
                if (parts.Length > 1) name = parts[1].Trim();
            }

            if (string.IsNullOrEmpty(name))
            {
                var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0) name = parts[^1];
            }

            NameInput.Text = name;

            string logoUrl = "";
            var logoMatches = Regex.Matches(html, @"""([^""]+/(?:StoreLogo|AppIcon|logo|icon|WindowIcon|PackageIcons|Store|Assets)[^""]+\.(?:png|jpg|jpeg|ico|svg))""");
            foreach (Match match in logoMatches)
            {
                string matchedUrl = match.Groups[1].Value;
                if (!matchedUrl.StartsWith("http"))
                {
                    string cleanedUrl = url.Replace("https://github.com/", "").TrimEnd('/');
                    var urlParts = cleanedUrl.Split('/');
                    if (urlParts.Length >= 2)
                    {
                        string owner = urlParts[0];
                        string repo = urlParts[1];
                        string localPath = Regex.Replace(matchedUrl, @"^/[^/]+/[^/]+/(?:blob|raw)/[^/]+/", "").TrimStart('/');
                        logoUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/refs/heads/main/{localPath}";
                        break;
                    }
                }
                else
                {
                    logoUrl = matchedUrl;
                    break;
                }
            }

            if (logoUrl.Contains("github.com") && logoUrl.Contains("/blob/"))
                logoUrl = logoUrl.Replace("github.com", "raw.githubusercontent.com").Replace("/blob/", "/refs/heads/");

            LogoUrlInput.Text = logoUrl;
            ShowStatus("GitHub metadata resolved.", InfoBarSeverity.Success);
        }

        private void ParseMicrosoftStoreMetadata(string html)
        {
            string name = "";
            var nameMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                name = nameMatch.Groups[1].Value;
                int idx = name.IndexOf(" - Free");
                if (idx > 0) name = name.Substring(0, idx);
                idx = name.IndexOf(" - Official");
                if (idx > 0) name = name.Substring(0, idx);
                name = name.Split(" | ")[0].Trim();
            }

            NameInput.Text = name;

            string logoUrl = "";
            var imageMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (imageMatch.Success) logoUrl = imageMatch.Groups[1].Value;

            LogoUrlInput.Text = logoUrl;
            ShowStatus("Microsoft Store metadata resolved.", InfoBarSeverity.Success);
        }

        public void PopulateForm(FlatAppItem item)
        {
            UrlInput.Text = item.Url;
            NameInput.Text = item.Name;
            LogoUrlInput.Text = item.LogoUrl;
            _selectedCategory = item.GroupKey;
            CategoryButtonText.Text = string.IsNullOrEmpty(item.GroupKey) ? "Select a category..." : item.GroupKey;

            if (item.IsFoss)
                PricingSegment.SelectedIndex = 1; // FOSS
            else if (item.IsPaid)
                PricingSegment.SelectedIndex = 2; // Paid
            else
                PricingSegment.SelectedIndex = 0; // Free

            if (item.IsPlanned)
                ProjectStateSegment.SelectedIndex = 1; // Planned
            else if (item.Indicator == "WDA")
                ProjectStateSegment.SelectedIndex = 2; // Discontinued
            else
                ProjectStateSegment.SelectedIndex = 0; // Active

            // Match indicator segment
            for (int i = 0; i < IndicatorSegment.Items.Count; i++)
            {
                if (IndicatorSegment.Items[i] is SegmentedItem segItem && segItem.Tag?.ToString() == item.Indicator)
                {
                    IndicatorSegment.SelectedIndex = i;
                    break;
                }
            }

            UpdatePreview();
        }

        public void ClearForm()
        {
            UrlInput.Text = "";
            NameInput.Text = "";
            LogoUrlInput.Text = "";
            _selectedCategory = "";
            CategoryButtonText.Text = "Select a category...";

            if (PricingSegment != null) PricingSegment.SelectedIndex = 0; // Free
            if (ProjectStateSegment != null) ProjectStateSegment.SelectedIndex = 0; // Active
            IndicatorSegment.SelectedIndex = 0;

            StatusInfo.IsOpen = false;
            FetchSuccessIcon.Visibility = Visibility.Collapsed;

            // Clear search box too
            if (CategorySearchBox != null) CategorySearchBox.Text = "";

            UpdatePreview();
        }

        private void CategorySearchBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            FilterCategoryTree(sender.Text);
        }

        private void FilterCategoryTree(string query)
        {
            var mainWindow = App.MainWindowInstance;
            if (mainWindow == null) return;

            var allNodes = System.Linq.Enumerable.ToList(
                System.Linq.Enumerable.Where(mainWindow.CategoryNodes,
                    n => !n.Tag.Contains("Newly Added Apps") && !n.Name.Contains("Newly Added Apps"))
            );

            if (string.IsNullOrWhiteSpace(query))
            {
                CategoryTree.ItemsSource = allNodes;
                return;
            }

            string q = query.Trim().ToLower();
            var filteredList = new System.Collections.Generic.List<CategoryNode>();

            foreach (var parent in allNodes)
            {
                bool parentMatches = parent.Name.ToLower().Contains(q);
                var matchingChildren = new System.Collections.Generic.List<CategoryNode>();

                foreach (var child in parent.Children)
                {
                    if (parentMatches || child.Name.ToLower().Contains(q))
                    {
                        matchingChildren.Add(child);
                    }
                }

                if (parentMatches || matchingChildren.Count > 0)
                {
                    var clonedParent = new CategoryNode
                    {
                        Name = parent.Name,
                        Tag = parent.Tag,
                        IconPath = parent.IconPath,
                        IsExpanded = true
                    };
                    foreach (var child in matchingChildren)
                    {
                        clonedParent.Children.Add(child);
                    }
                    filteredList.Add(clonedParent);
                }
            }

            CategoryTree.ItemsSource = filteredList;
        }
    }
}
