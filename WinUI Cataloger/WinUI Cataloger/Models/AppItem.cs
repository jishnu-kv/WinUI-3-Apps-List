using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System;

namespace WinUI_Cataloger.Models
{
    public class AppItem
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Indicator { get; set; } = "WDM";
        public string LogoUrl { get; set; } = "";

        private string? _displayLogoUrl;
        public string DisplayLogoUrl => _displayLogoUrl ??=
            (string.IsNullOrWhiteSpace(LogoUrl) || LogoUrl == "nan")
                ? "ms-appx:///Assets/StoreLogo.scale-200.png"
                : LogoUrl;

        public bool IsFoss { get; set; }
        public bool IsPaid { get; set; }
        public bool IsPlanned { get; set; }

        public Uri NavigateUri => Uri.TryCreate(Url, UriKind.Absolute, out var uri) ? uri : new Uri("about:blank");

        public Visibility IsFossVisibility => IsFoss ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPaidVisibility => IsPaid ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPlannedVisibility => IsPlanned ? Visibility.Visible : Visibility.Collapsed;

        // Fallback logo logic
        public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl) && LogoUrl != "nan";
        public Visibility HasLogoVisibility => HasLogo ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FallbackVisibility => HasLogo ? Visibility.Collapsed : Visibility.Visible;
        public string FirstLetter => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpper();

        private static readonly string[] FallbackColors = {
            "#0078D4", "#107C41", "#D83B01", "#5C2D91", "#008272", "#B4009E", "#8764B8", "#A07812"
        };

        // Cached brushes — computed once, never re-allocated on each UI pass
        private SolidColorBrush? _fallbackBackgroundBrush;
        public Brush FallbackBackgroundBrush
        {
            get
            {
                if (_fallbackBackgroundBrush != null) return _fallbackBackgroundBrush;
                int hash = 0;
                foreach (char c in Name) hash += c;
                string hexColor = FallbackColors[Math.Abs(hash) % FallbackColors.Length];
                _fallbackBackgroundBrush = new SolidColorBrush(ColorFromHex(hexColor));
                return _fallbackBackgroundBrush;
            }
        }

        // Payment/Planned Badges
        public string PaymentBadgeText => IsPlanned ? "Planned" : (IsPaid ? "Paid" : (IsFoss ? "FOSS" : "Free"));

        private SolidColorBrush? _paymentBadgeBackground;
        public Brush PaymentBadgeBackground
        {
            get
            {
                if (_paymentBadgeBackground != null) return _paymentBadgeBackground;
                string hex = IsPlanned ? "#1B2A3A" : (IsPaid ? "#3D2314" : (IsFoss ? "#1B2F1B" : "#2A2A2A"));
                _paymentBadgeBackground = new SolidColorBrush(ColorFromHex(hex));
                return _paymentBadgeBackground;
            }
        }

        private SolidColorBrush? _paymentBadgeForeground;
        public Brush PaymentBadgeForeground
        {
            get
            {
                if (_paymentBadgeForeground != null) return _paymentBadgeForeground;
                string hex = IsPlanned ? "#00A2EE" : (IsPaid ? "#FF8C00" : (IsFoss ? "#6CCB5F" : "#989898"));
                _paymentBadgeForeground = new SolidColorBrush(ColorFromHex(hex));
                return _paymentBadgeForeground;
            }
        }

        private static Color ColorFromHex(string hex)
        {
            hex = hex.Replace("#", "");
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
    }
}
