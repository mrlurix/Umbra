using System;
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
    public class FileItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isHidden;

        public FileItem()
        {
            TranslationSource.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(StatusText));
            };
        }

        public string FullPath { get; set; } = "";
        public string DisplayName => Path.GetFileName(FullPath);
        public string DisplayPath => Path.GetDirectoryName(FullPath) ?? FullPath;
        public bool IsDirectory => Directory.Exists(FullPath);
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
            if (e.ChangedButton == MouseButton.Left) DragMove();
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
            var q = SearchBox.Text.Trim().ToLower();
            ItemList.Items.Filter = (obj) =>
            {
                if (string.IsNullOrEmpty(q)) return true;
                if (obj is FileItem fi)
                    return fi.DisplayName.ToLower().Contains(q) || fi.DisplayPath.ToLower().Contains(q);
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
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var path = Path.Combine(desktop, $"Umbra_list_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var lines = _items.Select(i => $"{(i.IsHidden ? "[H]" : "[V]")} {i.FullPath}");
                File.WriteAllLines(path, lines);
                ShowToast(TranslationSource.Instance.ToastExported, true);
            }
            catch (Exception ex)
            {
                ShowToast($"\u26A0\uFE0F {ex.Message}", false);
            }
        }

        // ---- persistence ----
        private void SaveItems()
        {
            try
            {
                var data = _items.Select(i => new ItemData(i.FullPath, i.IsHidden, i.Size)).ToList();
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFile, json);
            }
            catch { }
        }

        private void LoadItems()
        {
            try
            {
                if (!File.Exists(_dataFile)) return;
                var json = File.ReadAllText(_dataFile);
                var data = JsonSerializer.Deserialize<ItemData[]>(json);
                if (data == null) return;
                _items.Clear();
                foreach (var d in data)
                {
                    if (!File.Exists(d.FullPath) && !Directory.Exists(d.FullPath)) continue;
                    _items.Add(new FileItem
                    {
                        FullPath = d.FullPath,
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

            var root = dlg.SelectedPath;
            int found = 0;

            try
            {
                foreach (var f in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if ((File.GetAttributes(f) & FileAttributes.Hidden) == FileAttributes.Hidden &&
                            !_items.Any(i => i.FullPath.Equals(f, StringComparison.OrdinalIgnoreCase)))
                        {
                            _items.Add(new FileItem
                            {
                                FullPath = f,
                                IsHidden = true,
                                Size = new FileInfo(f).Length
                            });
                            found++;
                        }
                    }
                    catch { }
                }
                foreach (var d in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if ((File.GetAttributes(d) & FileAttributes.Hidden) == FileAttributes.Hidden &&
                            !_items.Any(i => i.FullPath.Equals(d, StringComparison.OrdinalIgnoreCase)))
                        {
                            _items.Add(new FileItem { FullPath = d, IsHidden = true });
                            found++;
                        }
                    }
                    catch { }
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
            int n = 0;
            foreach (var p in paths)
            {
                if (_items.Any(i => i.FullPath.Equals(p, StringComparison.OrdinalIgnoreCase))) continue;
                try
                {
                    if (!File.Exists(p) && !Directory.Exists(p)) continue;
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
            int ok = 0, fail = 0;
            foreach (var item in sel)
            {
                try
                {
                    var p = item.FullPath;
                    if (!File.Exists(p) && !Directory.Exists(p)) { fail++; continue; }
                    var a = File.GetAttributes(p);
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
                    File.SetAttributes(p, a);
                    item.IsHidden = hide;
                    ok++;
                }
                catch { fail++; }
            }
            string actionKey = hide ? (super ? "ToastSuperHideAction" : "ToastHideAction") : "ToastUnhideAction";
            string msg = $"\u2705 {ok} {ts.T(actionKey)}";
            if (fail > 0) msg += $" | {fail} {ts.ToastErrors}";
            ShowToast(msg, fail == 0);
            RefreshStats();
            SaveItems();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var ts = TranslationSource.Instance;
            var sel = _items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) { ShowToast(ts.ToastNoSelection, false); return; }
            foreach (var item in sel) _items.Remove(item);
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
            SettingsStatTotalLabel.Text = string.Format(ts.T("StatTotalCount"), _items.Count);
            SettingsStatHiddenLabel.Text = string.Format(ts.T("StatHiddenCount"), hidden);
            SettingsStatVisibleLabel.Text = string.Format(ts.T("StatVisibleCount"), visible);
        }

        private void UpdateEmptyState() =>
            EmptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void ShowToast(string msg, bool success)
        {
            ToastIcon.Text = success ? "✅" : "⚠️";
            ToastText.Text = msg;
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
            base.OnClosing(e);
        }
    }
}
