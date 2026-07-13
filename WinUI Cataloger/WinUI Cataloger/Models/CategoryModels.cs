using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;

namespace WinUI_Cataloger.Models
{
    public class AppCategory
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string IconPath { get; set; } = "";
        public System.Uri? IconUri => System.Uri.TryCreate(IconPath, System.UriKind.Absolute, out var uri) ? uri : null;
        public ObservableCollection<AppSubCategory> SubCategories { get; } = new();
        public ObservableCollection<AppItem> Apps { get; } = new();
        public bool HasSubCategories => SubCategories.Count > 0;

        public Visibility SubCategoriesVisibility => HasSubCategories ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DirectAppsVisibility => Apps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IconVisibility => !string.IsNullOrEmpty(IconPath) ? Visibility.Visible : Visibility.Collapsed;
    }

    public class AppSubCategory
    {
        public string Name { get; set; } = "";
        public ObservableCollection<AppItem> Apps { get; } = new();
    }

    public class CategoryNode
    {
        public string Name { get; set; } = "";
        public string Tag { get; set; } = "";
        public string IconPath { get; set; } = "";
        public System.Uri? IconUri => System.Uri.TryCreate(IconPath, System.UriKind.Absolute, out var uri) ? uri : null;
        public Visibility IconVisibility => !string.IsNullOrEmpty(IconPath) ? Visibility.Visible : Visibility.Collapsed;
        public bool IsExpanded { get; set; } = false;
        public ObservableCollection<CategoryNode> Children { get; } = new();
    }
}
