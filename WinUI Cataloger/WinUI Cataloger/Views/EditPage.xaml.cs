using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WinUI_Cataloger.Models;

namespace WinUI_Cataloger.Views
{
    public sealed partial class EditPage : Page
    {
        public ObservableCollection<FlatAppItem> BatchQueue { get; } = new();
        private string? _readmePath;

        private static readonly Dictionary<string, string> CategoryHeaderMap = new()
        {
            // Social & Communication
            { "Discord", "#### <img src=\"https://i.postimg.cc/HWrDZr8j/size-32-id-D2Nq-Kl85S8Ye.png\" width=\"21\" height=\"21\" /> Discord" },
            { "Mastodon", "#### <img src=\"https://i.postimg.cc/vZdJZXqx/size-32-id-ITKS1ol-YDy-Mw.png\" width=\"18\" height=\"18\" /> Mastodon" },
            { "Reddit", "#### <img src=\"https://i.postimg.cc/50jGRr0n/size-32-id-h3FOPWMfg-Nn-V.png\" width=\"21\" height=\"21\" /> Reddit" },
            { "Telegram", "#### <img src=\"https://i.postimg.cc/j271sJyz/size-32-id-o-Wiu-H0j-Fi-U0R-format-png.png\" width=\"21\" height=\"21\" /> Telegram" },
            { "Twitter", "#### <img src=\"https://i.postimg.cc/521rVyyw/size-32-id-Kx-Hlias9Ag-Zt-format-png.png\" width=\"21\" height=\"21\" /> Twitter" },
            { "VK", "#### <img src=\"https://i.postimg.cc/rmvhYF78/size-32-id-KKJp-GUq-O7Pvk.png\" width=\"21\" height=\"21\" /> VK" },
            { "Social Media", "### 👨‍💻 Social Media" },
            { "Email Clients", "### 📧 Email Clients" },
            { "Translators", "### 🈵 Translators" },
            { "Transcribe", "### 🈵 Transcribe" },

            // Personalization
            { "Personalization", "### 🎨 Personalization" },

            // Media & Entertainment
            { "Music Players", "#### 🎧 Music Players" },
            { "Video Players", "#### ▶️ Video Players" },
            { "Spotify Client", "#### <img src=\"https://i.postimg.cc/tgk2yNwy/size-32-id-iefk-XAGb-Jma-P.png\" width=\"21\" height=\"21\" /> Spotify Client" },
            { "YT Music Client", "#### <img src=\"https://i.postimg.cc/J48sk6yQ/size-100-id-V1cb-DTh-Dpb-Rc-format-png-color-000000.png\" width=\"21\" height=\"21\" /> YT Music Client" },
            { "Streaming Services", "#### 📺 Streaming Services" },
            { "Tracking Services", "#### 📺 Tracking Services" },
            { "Podcast", "#### 🎙️ Podcast" },
            { "Photo Viewer", "#### 🏜️ Photo Viewer" },
            { "PDF Viewer", "#### 📄 PDF Viewer" },
            { "Other Players", "#### ▶️ Other Players" },
            { "Entertainment", "### 📱 Entertainment" },

            // Notes / To-do / Wish-lists
            { "Draw Board", "#### ⬜ Draw Board" },
            { "Notes", "#### 📝 Notes" },
            { "Reminders", "#### 🔔 Reminders" },
            { "To-Do / Task", "#### 🔔 To-Do / Task" },

            // Business / Books / Education / Productivity
            { "Business", "### 💼 Business" },
            { "Books & Reference", "### 📕 Books & Reference" },
            { "Education", "### 🎓 Education" },
            { "Productivity", "### 📈 Productivity" },

            // Power Tools & Utilities
            { "File Manager", "### 📁 File Manager" },
            { "Application Store", "### 🛍️ Application Store" },
            { "Calculators", "#### ➗ Calculators" },
            { "Device Info / Monitors", "#### 📊 Device Info / Monitors" },
            { "Optimizer / Cleaners", "#### 🧹 Optimizer / Cleaners" },
            { "Security & Privacy", "#### 🔒 Security & Privacy" },
            { "Finance", "### 🎙️ Finance" },

            // Web & Connectivity
            { "Browser", "### 🌐 Browser" },
            { "Download Managers", "### ⬇️ Download Managers" },
            { "Download Managers › Full-Featured Download Manager", "#### Full-Featured Download Manager" },
            { "Full-Featured Download Manager", "#### Full-Featured Download Manager" },
            { "Download Managers › Torrenting", "#### Torrenting" },
            { "Torrenting", "#### Torrenting" },
            { "Download Managers › YouTube", "#### YouTube" },
            { "YouTube", "#### YouTube" },
            { "Download Managers › Other", "#### Other" },

            // Developer & Power User
            { "Developer Tools › GitHub Client", "#### <img src=\"https://i.postimg.cc/5N9924Xd/size-32-id-AZOZNn-Y73haj-format-png.png\" width=\"21\" height=\"21\" /> GitHub Client" },
            { "GitHub Client", "#### <img src=\"https://i.postimg.cc/5N9924Xd/size-32-id-AZOZNn-Y73haj-format-png.png\" width=\"21\" height=\"21\" /> GitHub Client" },
            { "Developer Tools › Other", "#### <img src=\"https://i.postimg.cc/dtgfSnPS/size-32-id-Pk-Qp-LJis-Pi-TI-format-png.png\" width=\"21\" height=\"21\" /> Other" },
            { "Other Dev Tools", "#### <img src=\"https://i.postimg.cc/dtgfSnPS/size-32-id-Pk-Qp-LJis-Pi-TI-format-png.png\" width=\"21\" height=\"21\" /> Other" },
            { "Artificial Intelligence (AI)", "### 🤖 Artificial Intelligence (AI)" },
            { "Multimedia & Design", "### 💠 Multimedia & Design" },
            { "Catalogs", "### 📦 Catalogs" },
            { "Calendar", "### 📅 Calendar" },

            // Health & Lifestyle
            { "Lifestyle", "### 🙎‍♂️ Lifestyle" },
            { "News", "### 📰 News" },
            { "Weather", "### 🌦️ Weather" },

            // Games
            { "Games", "### 🎮 Games" },
            { "Game Tools", "#### Game Tools" },

            // Other general
            { "Windows Apps", "### 🪟 Windows Apps" },
            { "Miscellaneous", "### 📖 Miscellaneous" },
            { "Utilities", "### 🔧 Utilities" }
        };

        public EditPage()
        {
            InitializeComponent();
            _readmePath = FindReadmePath();
            ContentFrame.Navigate(typeof(AddAppsPage), this);
        }

        private string? FindReadmePath()
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


        private async void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatchQueue.Count == 0)
            {
                ShowStatus("Batch queue is empty.", InfoBarSeverity.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_readmePath) || !File.Exists(_readmePath))
            {
                ShowStatus("Could not find README.md path.", InfoBarSeverity.Error);
                return;
            }

            try
            {
                string readmeContent = File.ReadAllText(_readmePath);
                int addedCount = 0;

                foreach (var app in BatchQueue)
                {
                    // Map raw category key from clean name
                    string cleanCat = app.GroupKey;
                    string lookupKey = cleanCat;

                    if (cleanCat.Contains(" › "))
                    {
                        string[] parts = cleanCat.Split(" › ");
                        string subCat = parts[^1];
                        if (CategoryHeaderMap.ContainsKey(cleanCat))
                        {
                            lookupKey = cleanCat;
                        }
                        else if (CategoryHeaderMap.ContainsKey(subCat))
                        {
                            lookupKey = subCat;
                        }
                    }
                    else
                    {
                        foreach (var kvp in CategoryHeaderMap)
                        {
                            if (cleanCat.Contains(kvp.Key))
                            {
                                lookupKey = kvp.Key;
                                break;
                            }
                        }
                    }

                    string categoryHeader = CategoryHeaderMap.TryGetValue(lookupKey, out var header) ? header : cleanCat;
                    int categoryIndex = readmeContent.IndexOf(categoryHeader);
                    if (categoryIndex == -1)
                    {
                        ShowStatus($"Error: Could not find category header '{categoryHeader}' in README.md. Please verify the category spelling/mapping.", InfoBarSeverity.Error);
                        return;
                    }

                    int nextHeaderIndex = readmeContent.IndexOf("\n###", categoryIndex + categoryHeader.Length);
                    if (nextHeaderIndex == -1) nextHeaderIndex = readmeContent.IndexOf("\n---", categoryIndex + categoryHeader.Length);
                    if (nextHeaderIndex == -1) nextHeaderIndex = readmeContent.Length;

                    string sectionText = readmeContent.Substring(categoryIndex, nextHeaderIndex - categoryIndex);
                    var sectionLines = sectionText.Split('\n').ToList();

                    // Generate markdown line
                    string fossBadge = app.IsFoss ? " <sup>`FOSS`</sup>" : "";
                    string paidBadge = app.IsPaid ? " `💰`" : "";
                    string plannedBadge = app.IsPlanned ? " `📆 Planned`" : "";
                    string logoComment = !string.IsNullOrWhiteSpace(app.LogoUrl) ? $" <!-- logo: {app.LogoUrl} -->" : "";
                    string newAppLine = $"- `{app.Indicator}` [{app.Name}]({app.Url}){paidBadge}{plannedBadge}{fossBadge}{logoComment}";

                    // Insert alphabetically
                    int insertLineIdx = -1;
                    for (int i = 0; i < sectionLines.Count; i++)
                    {
                        string line = sectionLines[i].Trim();
                        if (line.StartsWith("- ") || line.StartsWith("- `"))
                        {
                            var nameMatch = Regex.Match(line, @"\[([^\]]+)\]");
                            if (nameMatch.Success)
                            {
                                string existingName = nameMatch.Groups[1].Value;
                                if (string.Compare(app.Name, existingName, StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    insertLineIdx = i;
                                    break;
                                }
                            }
                        }
                    }

                    if (insertLineIdx != -1)
                        sectionLines.Insert(insertLineIdx, newAppLine);
                    else
                    {
                        int lastListIdx = -1;
                        for (int i = sectionLines.Count - 1; i >= 0; i--)
                        {
                            if (sectionLines[i].Trim().StartsWith("- ")) { lastListIdx = i; break; }
                        }
                        if (lastListIdx != -1) sectionLines.Insert(lastListIdx + 1, newAppLine);
                        else sectionLines.Add(newAppLine);
                    }

                    string updatedSection = string.Join("\n", sectionLines);
                    readmeContent = readmeContent.Replace(sectionText, updatedSection);

                    // Insert into newly added section
                    string newlyAddedHeader = "### 🆕 Newly Added Apps!";
                    int newAddedIdx = readmeContent.IndexOf(newlyAddedHeader);
                    if (newAddedIdx != -1)
                    {
                        int nextSectionIdx = readmeContent.IndexOf("\n<sub>[📑 Table Of Contents", newAddedIdx);
                        if (nextSectionIdx != -1)
                        {
                            string newAddedSection = readmeContent.Substring(newAddedIdx, nextSectionIdx - newAddedIdx);
                            var newAddedLines = newAddedSection.Split('\n').ToList();

                            int newInsertIdx = -1;
                            for (int i = 0; i < newAddedLines.Count; i++)
                            {
                                string line = newAddedLines[i].Trim();
                                if (line.StartsWith("- ") || line.StartsWith("- `"))
                                {
                                    var nameMatch = Regex.Match(line, @"\[([^\]]+)\]");
                                    if (nameMatch.Success && string.Compare(app.Name, nameMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase) < 0)
                                    {
                                        newInsertIdx = i;
                                        break;
                                    }
                                }
                            }

                            if (newInsertIdx != -1) newAddedLines.Insert(newInsertIdx, newAppLine);
                            else
                            {
                                int lastListIdx = -1;
                                for (int i = newAddedLines.Count - 1; i >= 0; i--)
                                {
                                    if (newAddedLines[i].Trim().StartsWith("- ")) { lastListIdx = i; break; }
                                }
                                if (lastListIdx != -1) newAddedLines.Insert(lastListIdx + 1, newAppLine);
                                else newAddedLines.Add(newAppLine);
                            }

                            readmeContent = readmeContent.Replace(newAddedSection, string.Join("\n", newAddedLines));
                        }
                    }

                    addedCount++;
                }

                // Update README.md counter
                var countMatch = Regex.Match(readmeContent, @"Last (\d+) apps that were recently added to list!");
                if (countMatch.Success)
                {
                    int currentCount = int.Parse(countMatch.Groups[1].Value);
                    readmeContent = readmeContent.Replace(countMatch.Value, $"Last {currentCount + addedCount} apps that were recently added to list!");
                }

                File.WriteAllText(_readmePath, readmeContent);
                BatchQueue.Clear();
                ShowStatus("Successfully wrote to README.md. Regenerating database JSON...", InfoBarSeverity.Informational);

                await RunGenerateAppsScriptAsync();

                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }

                ShowStatus($"Successfully wrote {addedCount} apps to README.md and updated JSON database!", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed saving batch: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task RunGenerateAppsScriptAsync()
        {
            if (string.IsNullOrEmpty(_readmePath)) return;
            string repoRoot = Path.GetDirectoryName(_readmePath) ?? "";
            string scriptPath = Path.Combine(repoRoot, "WinUI Cataloger", "WinUI Cataloger", "scripts", "generate-apps.py");

            if (!File.Exists(scriptPath)) return;

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"\"{scriptPath}\"",
                        WorkingDirectory = repoRoot,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit();
                        }
                    }

                    // Copy newly generated JSON and downloaded WebP images to BaseDirectory output folder
                    string projectAssetsDir = Path.Combine(repoRoot, "WinUI Cataloger", "WinUI Cataloger", "Assets");
                    string targetAssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

                    if (Directory.Exists(projectAssetsDir))
                    {
                        // Copy apps-data.json
                        string srcJson = Path.Combine(projectAssetsDir, "data", "apps-data.json");
                        string destJson = Path.Combine(targetAssetsDir, "data", "apps-data.json");
                        if (File.Exists(srcJson))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destJson)!);
                            File.Copy(srcJson, destJson, true);
                        }

                        // Copy images from Assets/apps
                        string srcApps = Path.Combine(projectAssetsDir, "apps");
                        string destApps = Path.Combine(targetAssetsDir, "apps");
                        if (Directory.Exists(srcApps))
                        {
                            Directory.CreateDirectory(destApps);
                            foreach (var file in Directory.GetFiles(srcApps))
                            {
                                string destFile = Path.Combine(destApps, Path.GetFileName(file));
                                try { File.Copy(file, destFile, true); } catch { }
                            }
                        }
                    }
                }
                catch { }
            });
        }

        public void ShowStatus(string message, InfoBarSeverity severity)
        {
            SaveStatusBar.Message = message;
            SaveStatusBar.Severity = severity;
            SaveStatusBar.IsOpen = true;
        }

        public void HideStatus()
        {
            SaveStatusBar.IsOpen = false;
        }

        public bool IsStatusErrorOpen => SaveStatusBar.IsOpen && SaveStatusBar.Severity == InfoBarSeverity.Error;
    }
}
