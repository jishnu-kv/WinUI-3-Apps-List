using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;

namespace WinUI_Cataloger.Models
{
    /// <summary>
    /// A flat app item extended with its group display name and sub-group label,
    /// used to feed the grouped CollectionViewSource for virtualized ListView rendering.
    /// </summary>
    public class FlatAppItem : AppItem
    {
        /// <summary>Category heading key (raw, for scroll-to lookup)</summary>
        public string CategoryKey { get; set; } = "";

        /// <summary>Displayed group header: "Category > SubCategory" or just "Category"</summary>
        public string GroupKey { get; set; } = "";

        /// <summary>Icon URI for the parent category (shown in group header)</summary>
        public string CategoryIconPath { get; set; } = "";
        public System.Uri? CategoryIconUri =>
            System.Uri.TryCreate(CategoryIconPath, System.UriKind.Absolute, out var u) ? u : null;
        public Visibility CategoryIconVisibility =>
            !string.IsNullOrEmpty(CategoryIconPath) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>Optional sub-group label shown below the category header (empty for top-level)</summary>
        public string SubGroupLabel { get; set; } = "";
        public Visibility SubGroupLabelVisibility =>
            !string.IsNullOrEmpty(SubGroupLabel) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>Flag to indicate this app is newly added in the batch queue (for preview highlighting)</summary>
        public bool IsNew { get; set; } = false;
        public Visibility IsNewVisibility => IsNew ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Represents a group of FlatAppItems that share the same GroupKey.
    /// Used as the grouped source for the ListView.
    /// </summary>
    public class AppGroup : ObservableCollection<FlatAppItem>
    {
        public string Key { get; }
        public string DisplayName { get; }
        public string IconPath { get; }
        public System.Uri? IconUri =>
            System.Uri.TryCreate(IconPath, System.UriKind.Absolute, out var u) ? u : null;
        public Visibility IconVisibility =>
            !string.IsNullOrEmpty(IconPath) ? Visibility.Visible : Visibility.Collapsed;

        public AppGroup(string key, string displayName, string iconPath)
        {
            Key = key;
            DisplayName = displayName;
            IconPath = iconPath;
        }
    }
}
