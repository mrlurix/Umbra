using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using Umbra.Lang;

namespace Umbra
{
    public class FileItem : INotifyPropertyChanged, IDisposable
    {
        private bool _isSelected;
        private bool _isHidden;
        private bool _disposed;

        public FileItem()
        {
            TranslationSource.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged() => OnPropertyChanged(nameof(StatusText));

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            TranslationSource.LanguageChanged -= OnLanguageChanged;
            GC.SuppressFinalize(this);
        }

        public string FullPath { get; set; } = "";
        public string DisplayName
        {
            get
            {
                try { return Path.GetFileName(FullPath) ?? FullPath; }
                catch { return FullPath; }
            }
        }
        public string DisplayPath
        {
            get
            {
                try { return Path.GetDirectoryName(FullPath) ?? FullPath; }
                catch { return FullPath; }
            }
        }
        public bool IsDirectory
        {
            get
            {
                try { return Directory.Exists(FullPath) && !File.Exists(FullPath); }
                catch { return false; }
            }
        }
        public string Icon => IsDirectory ? "\U0001F4C1" : "\U0001F4C4";

        public long Size { get; set; }
        public string SizeText
        {
            get
            {
                if (IsDirectory) return "";
                if (Size < 1024) return $"{Size} B";
                if (Size < 1024 * 1024) return $"{Size / 1024.0:F1} KB";
                return $"{Size / (1024.0 * 1024):F1} MB";
            }
        }

        public Brush IconBg => IsHidden
            ? new SolidColorBrush(Color.FromArgb(0x30, 0xEF, 0x44, 0x44))
            : new SolidColorBrush(Color.FromArgb(0x30, 0x22, 0xC5, 0x5E));

        public string StatusText
        {
            get
            {
                var ts = TranslationSource.Instance;
                if (IsHidden) return $"\U0001F648 {ts.T("StatusHidden")}";
                return $"\U0001F4C4 {ts.T("StatusVisible")}";
            }
        }

        public Brush BadgeBg => IsHidden
            ? new SolidColorBrush(Color.FromArgb(0x20, 0xEF, 0x44, 0x44))
            : new SolidColorBrush(Color.FromArgb(0x20, 0x22, 0xC5, 0x5E));

        public Brush BadgeText => IsHidden
            ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
            : new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));

        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (_isHidden != value)
                {
                    _isHidden = value;
                    OnPropertyChanged(nameof(IsHidden));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(IconBg));
                    OnPropertyChanged(nameof(BadgeBg));
                    OnPropertyChanged(nameof(BadgeText));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public record ItemData(string FullPath, bool IsHidden, long Size);

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<FileItem> _items = new();
        private bool _settingsOpen;
        private string _dataFile = "";
        private const int MaxSearchQueryLength = 200;

        public MainWindow()
        {
            InitializeComponent();
            ItemList.ItemsSource = _items;
            _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "items.json");

            Loaded += (s, e) =>
            {
                var anim = new DoubleAnimation
                {
                    From = 0, To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                BeginAnimation(OpacityProperty, anim);
                try { Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Umbra.ico")); } catch { }
                LoadItems();
            };

            KeyDown += (s, e) =>
            {
                var ts = TranslationSource.Instance;
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.H)
                { if (!e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift)) ToggleHide(true, false); else ToggleHide(true, true); e.Handled = true; }
                else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.U)
                { ToggleHide(false, false); e.Handled = true; }
                else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.A)
                { foreach (var i in _items) i.IsSelected = true; ShowToast(ts.ToastSelectedAll, true); e.Handled = true; }
                else if (e.Key == Key.Delete)
                { BtnRemove_Click(s, e); e.Handled = true; }
                else if (e.Key == Key.F5)
                { BtnRefresh_Click(s, e); e.Handled = true; }
                else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.S)
                { SaveItems(); ShowToast(ts.ToastSaved, true); e.Handled = true; }
                else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.O)
                { LoadItems(); ShowToast(ts.ToastLoaded, true); e.Handled = true; }
            };
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchHint.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Hidden;
            BtnClearSearch.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Hidden : Visibility.Visible;
            FilterItems();
        }
        private void BtnClearSearch_Click(object sender, RoutedEventArgs e) { SearchBox.Clear(); SearchBox.Focus(); }

        private void FilterItems()
        {
            var raw = SearchBox.Text ?? "";
            if (raw.Length > MaxSearchQueryLength) raw = raw.Substring(0, MaxSearchQueryLength);
            var q = raw.Trim().ToLowerInvariant();
            ItemList.Items.Filter = (obj) =>
            {
                if (string.IsNullOrEmpty(q)) return true;
                if (obj is FileItem fi)
                {
                    try
                    {
                        return fi.DisplayName.ToLowerInvariant().Contains(q) || fi.DisplayPath.ToLowerInvariant().Contains(q);
                    }
                    catch { return false; }
                }
                return true;
            };
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsOpen = !_settingsOpen;
            SettingsPanel.Visibility = Visibility.Visible;

            var sb = new Storyboard();
            var slideAnim = new ThicknessAnimation
            {
                To = new Thickness(_settingsOpen ? 0 : 260, 0, 0, 0),
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideAnim, SettingsPanel);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("Margin"));

            var opacityAnim = new DoubleAnimation
            {
                To = _settingsOpen ? 1.0 : 0.0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTarget(opacityAnim, SettingsPanel);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            sb.Children.Add(slideAnim);
            sb.Children.Add(opacityAnim);

            if (!_settingsOpen)
                sb.Completed += (_, _) => { if (!_settingsOpen) SettingsPanel.Visibility = Visibility.Hidden; };

            sb.Begin(this);
        }

        private void BtnCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsOpen) BtnSettings_Click(sender, e);
        }

        private void LangToggle_Click(object sender, RoutedEventArgs e)
        {
            var ts = TranslationSource.Instance;
            ts.Lang = ts.Lang == "fa" ? "en" : "fa";
        }

        private void BtnSelectAllSettings_Click(object sender, RoutedEventArgs e)
        {
            foreach (var i in _items) i.IsSelected = true;
            ShowToast(TranslationSource.Instance.ToastSelectedAll, true);
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0) { ShowToast(TranslationSource.Instance.ToastNoSelection, false); return; }
            var ts = TranslationSource.Instance;
            var msg = ts.Lang == "fa" ? "آیا از پاک کردن تمام آیتم‌ها مطمئن هستید؟" : "Are you sure to clear all items?";
            var caption = ts.Lang == "fa" ? "تأیید" : "Confirm";
            if (MessageBox.Show(msg, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            foreach (var it in _items) it.Dispose();
            _items.Clear();
            ShowToast(TranslationSource.Instance.ToastCleared, true);
            UpdateEmptyState();
            RefreshStats();
            SaveItems();
        }

        private void BtnExportList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SecurityHelper.ValidateItemsCount(_items.Count, out var cntErr))
                {
                    ShowToast(cntErr, false);
                    return;
                }
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
                {
                    ShowToast(TranslationSource.Instance.Lang == "fa" ? "مسیر دسکتاپ یافت نشد" : "Desktop path not found", false);
                    return;
                }
                var fileName = $"Umbra_list_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                // Sanitize filename (already safe, but ensure)
                foreach (var c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
                var path = Path.Combine(desktop, fileName);
                // Validate canonical path is still under desktop
                var canonDesktop = Path.GetFullPath(desktop).TrimEnd('\\') + "\\";
                var canonPath = Path.GetFullPath(path);
                if (!canonPath.StartsWith(canonDesktop, StringComparison.OrdinalIgnoreCase))
                {
                    ShowToast("مسیر نامعتبر", false);
                    return;
                }
                var lines = _items.Select(i => $"{(i.IsHidden ? "[H]" : "[V]")} {i.FullPath}").ToList();
                if (lines.Count > SecurityHelper.MaxItems) lines = lines.Take(SecurityHelper.MaxItems).ToList();
                File.WriteAllLines(path, lines);
                ShowToast(TranslationSource.Instance.ToastExported, true);
            }
            catch
            {
                // FIX: Don't leak exception details (information disclosure)
                ShowToast(TranslationSource.Instance.Lang == "fa" ? "خطا در خروجی گرفتن لیست" : "Failed to export list", false);
            }
        }

        // ---- persistence ----
        private void SaveItems()
        {
            try
            {
                if (!SecurityHelper.ValidateItemsCount(_items.Count, out _)) return;
                var data = _items.Select(i => new ItemData(i.FullPath, i.IsHidden, i.Size)).ToList();
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                if (!SecurityHelper.ValidateJsonSize(json, out _)) return;
                // FIX: Atomic write to prevent corruption / partial write attacks
                var tmp = _dataFile + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, _dataFile, true);
            }
            catch { }
        }

        private void LoadItems()
        {
            try
            {
                if (!File.Exists(_dataFile)) return;
                var info = new FileInfo(_dataFile);
                if (info.Length > SecurityHelper.MaxJsonSize) return; // prevent OOM
                var json = File.ReadAllText(_dataFile);
                if (!SecurityHelper.ValidateJsonSize(json, out _)) return;
                var options = new JsonSerializerOptions { AllowTrailingCommas = true };
                var data = JsonSerializer.Deserialize<ItemData[]>(json, options);
                if (data == null) return;
                if (!SecurityHelper.ValidateItemsCount(data.Length, out _)) return;
                // Dispose old items to prevent leak
                foreach (var old in _items) old.Dispose();
                _items.Clear();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in data)
                {
                    if (d == null) continue;
                    if (!SecurityHelper.ValidateItemData(d, out _)) continue;
                    if (!SecurityHelper.TryCanonicalize(d.FullPath, out var canon, out _)) continue;
                    if (!seen.Add(canon)) continue;
                    if (_items.Count >= SecurityHelper.MaxItems) break;
                    try
                    {
                        if (!File.Exists(canon) && !Directory.Exists(canon)) continue;
                        if (SecurityHelper.IsReparsePoint(canon)) continue; // skip symlinks/junctions
                    }
                    catch { continue; }
                    _items.Add(new FileItem
                    {
                        FullPath = canon,
                        IsHidden = d.IsHidden,
                        Size = d.Size
                    });
                }
            }
            catch { }
            RefreshStats();
            UpdateEmptyState();
        }

        // ---- scan ----
        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var ts = TranslationSource.Instance;
            var dlg = new System.Windows.Forms.FolderBrowserDialog { Description = ts.Lang == "fa" ? "انتخاب پوشه برای اسکن" : "Select folder to scan" };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            if (!SecurityHelper.TryCanonicalize(dlg.SelectedPath, out var root, out _))
            {
                ShowToast(ts.Lang == "fa" ? "مسیر نامعتبر" : "Invalid path", false);
                return;
            }
            if (!Directory.Exists(root))
            {
                ShowToast(ts.Lang == "fa" ? "پوشه یافت نشد" : "Folder not found", false);
                return;
            }
            if (SecurityHelper.IsCriticalSystemPath(root))
            {
                var warn = ts.Lang == "fa" ? "اسکن مسیرهای سیستمی حساس مجاز نیست" : "Scanning critical system paths is not allowed";
                ShowToast(warn, false);
                return;
            }

            int found = 0;
            const int MaxScanFound = 2000;
            var seen = new HashSet<string>(_items.Select(i => i.FullPath), StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();
            stack.Push(root);
            int dirsVisited = 0;
            const int MaxDirsVisited = 10000;

            try
            {
                while (stack.Count > 0 && found < MaxScanFound && dirsVisited < MaxDirsVisited)
                {
                    var current = stack.Pop();
                    dirsVisited++;
                    if (SecurityHelper.IsReparsePoint(current)) continue;

                    // Files in current
                    string[] files = Array.Empty<string>();
                    try { files = Directory.GetFiles(current); } catch (UnauthorizedAccessException) { continue; } catch { continue; }
                    foreach (var f in files)
                    {
                        if (found >= MaxScanFound) break;
                        try
                        {
                            if (SecurityHelper.IsReparsePoint(f)) continue;
                            if ((File.GetAttributes(f) & FileAttributes.Hidden) == FileAttributes.Hidden &&
                                !seen.Contains(f) &&
                                SecurityHelper.TryCanonicalize(f, out var cf, out _))
                            {
                                if (seen.Add(cf))
                                {
                                    _items.Add(new FileItem
                                    {
                                        FullPath = cf,
                                        IsHidden = true,
                                        Size = new FileInfo(f).Length
                                    });
                                    found++;
                                }
                            }
                        }
                        catch { }
                    }
                    if (found >= MaxScanFound) break;
                    // Directories
                    string[] dirs = Array.Empty<string>();
                    try { dirs = Directory.GetDirectories(current); } catch (UnauthorizedAccessException) { continue; } catch { continue; }
                    foreach (var d in dirs)
                    {
                        try
                        {
                            if (SecurityHelper.IsReparsePoint(d)) continue;
                            if ((File.GetAttributes(d) & FileAttributes.Hidden) == FileAttributes.Hidden &&
                                !seen.Contains(d) &&
                                SecurityHelper.TryCanonicalize(d, out var cd, out _))
                            {
                                if (seen.Add(cd))
                                {
                                    _items.Add(new FileItem { FullPath = cd, IsHidden = true });
                                    found++;
                                    if (found >= MaxScanFound) break;
                                }
                            }
                            // Push for deeper traversal even if not hidden, to find nested hidden
                            if (stack.Count < MaxDirsVisited) stack.Push(d);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            ShowToast(found > 0 ? string.Format(TranslationSource.Instance.ToastScanFound, found) : TranslationSource.Instance.ToastScanNone, found > 0);
            RefreshStats();
            UpdateEmptyState();
        }

        // ---- add ----
        private void AddItems(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            if (_items.Count >= SecurityHelper.MaxItems)
            {
                ShowToast(TranslationSource.Instance.Lang == "fa" ? $"حداکثر {SecurityHelper.MaxItems} آیتم مجاز است" : $"Max {SecurityHelper.MaxItems} items allowed", false);
                return;
            }
            int n = 0;
            foreach (var raw in paths)
            {
                if (_items.Count >= SecurityHelper.MaxItems) break;
                if (string.IsNullOrWhiteSpace(raw)) continue;
                if (!SecurityHelper.TryCanonicalize(raw, out var p, out _)) continue;
                if (_items.Any(i => i.FullPath.Equals(p, StringComparison.OrdinalIgnoreCase))) continue;
                try
                {
                    if (!File.Exists(p) && !Directory.Exists(p)) continue;
                    if (SecurityHelper.IsReparsePoint(p)) continue;
                    if (SecurityHelper.IsCriticalSystemPath(p)) continue;
                    bool h = (File.GetAttributes(p) & FileAttributes.Hidden) == FileAttributes.Hidden;
                    long sz = 0;
                    if (File.Exists(p)) sz = new FileInfo(p).Length;
                    _items.Add(new FileItem { FullPath = p, IsHidden = h, Size = sz });
                    n++;
                }
                catch { }
            }
            if (n > 0) ShowToast(string.Format(TranslationSource.Instance.ToastAdded, n), true);
            RefreshStats();
            UpdateEmptyState();
            SaveItems();
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            var ts = TranslationSource.Instance;
            var d = new OpenFileDialog { Multiselect = true, Title = ts.Lang == "fa" ? "انتخاب فایل‌ها" : "Select files", CheckFileExists = true };
            if (d.ShowDialog(this) == true) AddItems(d.FileNames);
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var ts = TranslationSource.Instance;
            using var d = new System.Windows.Forms.FolderBrowserDialog { Description = ts.Lang == "fa" ? "انتخاب پوشه" : "Select folder" };
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                AddItems(new[] { d.SelectedPath });
        }

        // ---- hide/unhide ----
        private void BtnHide_Click(object sender, RoutedEventArgs e) => ToggleHide(true, false);
        private void BtnSuperHide_Click(object sender, RoutedEventArgs e) => ToggleHide(true, true);
        private void BtnUnhide_Click(object sender, RoutedEventArgs e) => ToggleHide(false, false);

        private void ToggleHide(bool hide, bool super)
        {
            var ts = TranslationSource.Instance;
            var sel = _items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) { ShowToast(ts.ToastNoSelection, false); return; }

            // FIX: Pre-validate all selected items for critical paths before any operation
            var blocked = sel.Where(i => SecurityHelper.IsCriticalSystemPath(i.FullPath)).ToList();
            if (blocked.Count > 0)
            {
                var warn = ts.Lang == "fa" ? $"{blocked.Count} مسیر سیستمی حساس نادیده گرفته شد" : $"{blocked.Count} critical system paths skipped";
                ShowToast(warn, false);
                sel = sel.Except(blocked).ToList();
                if (sel.Count == 0) return;
            }

            // FIX: Confirmation for Super Hide (modifies System attribute, more dangerous)
            if (hide && super)
            {
                var msg = ts.Lang == "fa" ? $"آیا {sel.Count} آیتم با مخفی‌سازی قوی (Hidden+System) مخفی شود؟" : $"Strong hide {sel.Count} items with Hidden+System?";
                if (MessageBox.Show(msg, ts.Lang == "fa" ? "تأیید" : "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            }

            int ok = 0, fail = 0;
            foreach (var item in sel)
            {
                try
                {
                    var p = item.FullPath;
                    // FIX: Validate canonical path again (TOCTOU mitigation - re-validate)
                    if (!SecurityHelper.TryCanonicalize(p, out var canon, out _)) { fail++; continue; }
                    if (!File.Exists(canon) && !Directory.Exists(canon)) { fail++; continue; }
                    if (SecurityHelper.IsReparsePoint(canon)) { fail++; continue; }
                    var a = File.GetAttributes(canon);
                    if (hide)
                    {
                        a |= FileAttributes.Hidden;
                        if (super) a |= FileAttributes.System;
                        else a &= ~FileAttributes.System;
                    }
                    else
                    {
                        a &= ~FileAttributes.Hidden;
                        a &= ~FileAttributes.System;
                    }
                    File.SetAttributes(canon, a);
                    item.IsHidden = hide;
                    ok++;
                }
                catch { fail++; }
            }
            string actionKey = hide ? (super ? "ToastSuperHideAction" : "ToastHideAction") : "ToastUnhideAction";
            string msg2 = $"\u2705 {ok} {ts.T(actionKey)}";
            if (fail > 0) msg2 += $" | {fail} {ts.ToastErrors}";
            ShowToast(msg2, fail == 0);
            RefreshStats();
            SaveItems();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var ts = TranslationSource.Instance;
            var sel = _items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) { ShowToast(ts.ToastNoSelection, false); return; }
            foreach (var item in sel)
            {
                item.Dispose();
                _items.Remove(item);
            }
            ShowToast(string.Format(ts.ToastRemoved, sel.Count), true);
            UpdateEmptyState();
            RefreshStats();
            SaveItems();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
            {
                try
                {
                    if (!File.Exists(item.FullPath) && !Directory.Exists(item.FullPath)) continue;
                    if (SecurityHelper.IsReparsePoint(item.FullPath)) continue;
                    bool h = (File.GetAttributes(item.FullPath) & FileAttributes.Hidden) == FileAttributes.Hidden;
                    item.IsHidden = h;
                    if (File.Exists(item.FullPath)) item.Size = new FileInfo(item.FullPath).Length;
                }
                catch { }
            }
            ShowToast(TranslationSource.Instance.ToastRefreshed, true);
            RefreshStats();
        }

        private void ItemList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                BtnRemove_Click(sender, e);
        }

        // ---- ui helpers ----
        private void RefreshStats()
        {
            TxtTotalItems.Text = _items.Count.ToString();
            TxtHiddenItems.Text = _items.Count(i => i.IsHidden).ToString();
            TxtItemCount.Text = string.Format(TranslationSource.Instance.StatusItemCount, _items.Count);

            long totalSize = _items.Sum(i => i.Size);
            string sizeStr;
            if (totalSize < 1024) sizeStr = $"{totalSize} B";
            else if (totalSize < 1024 * 1024) sizeStr = $"{totalSize / 1024.0:F1} KB";
            else sizeStr = $"{totalSize / (1024.0 * 1024):F1} MB";
            TxtTotalSize.Text = sizeStr;

            var hidden = _items.Count(i => i.IsHidden);
            var visible = _items.Count - hidden;
            var ts = TranslationSource.Instance;
            SettingsStatTotal.Text = _items.Count.ToString();
            SettingsStatHidden.Text = hidden.ToString();
            SettingsStatVisible.Text = visible.ToString();
            try
            {
                SettingsStatTotalLabel.Text = string.Format(ts.T("StatTotalCount"), _items.Count);
                SettingsStatHiddenLabel.Text = string.Format(ts.T("StatHiddenCount"), hidden);
                SettingsStatVisibleLabel.Text = string.Format(ts.T("StatVisibleCount"), visible);
            }
            catch { }
        }

        private void UpdateEmptyState() =>
            EmptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void ShowToast(string msg, bool success)
        {
            // FIX: Sanitize message length to prevent UI overflow
            if (msg != null && msg.Length > 300) msg = msg.Substring(0, 300) + "...";
            ToastIcon.Text = success ? "✅" : "⚠️";
            ToastText.Text = msg ?? "";
            ToastBox.BorderBrush = success
                ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
                : new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
            ToastBox.Visibility = Visibility.Visible;
            ToastBox.Opacity = 0;

            var fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(180) };
            var slide = new ThicknessAnimation
            {
                To = new Thickness(20, 36, 0, 0),
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(250), BeginTime = TimeSpan.FromMilliseconds(1800) };
            var slideOut = new ThicknessAnimation { To = new Thickness(-60, 36, 0, 0), Duration = TimeSpan.FromMilliseconds(250), BeginTime = TimeSpan.FromMilliseconds(1800) };

            Storyboard.SetTarget(fadeIn, ToastBox); Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            Storyboard.SetTarget(slide, ToastBox); Storyboard.SetTargetProperty(slide, new PropertyPath("Margin"));
            Storyboard.SetTarget(fadeOut, ToastBox); Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            Storyboard.SetTarget(slideOut, ToastBox); Storyboard.SetTargetProperty(slideOut, new PropertyPath("Margin"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn); sb.Children.Add(slide);
            sb.Children.Add(fadeOut); sb.Children.Add(slideOut);
            sb.Completed += (_, _) => ToastBox.Visibility = Visibility.Hidden;
            sb.Begin(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveItems();
            foreach (var it in _items) it.Dispose();
            base.OnClosing(e);
        }
    }
}
