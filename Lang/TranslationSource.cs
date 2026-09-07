using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Umbra.Lang
{
    public class TranslationSource : INotifyPropertyChanged
    {
        private static readonly TranslationSource _instance = new();
        public static TranslationSource Instance => _instance;

        private string _lang = "en";

        public string Lang
        {
            get => _lang;
            set { _lang = value; NotifyAll(); }
        }

        public bool IsRTL => _lang == "fa";
        public FlowDirection Direction => IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        private static readonly Dictionary<string, Dictionary<string, string>> Data = new()
        {
            ["en"] = new()
            {
                ["HeaderText"] = "Umbra â€” File & Folder Manager",
                ["StatItems"] = "Items",
                ["StatHidden"] = "Hidden",
                ["StatTotalSize"] = "Total Size",
                ["StatusHidden"] = "Hidden",
                ["StatusVisible"] = "Visible",
                ["BtnAddFile"] = "\u2795 File",
                ["BtnAddFolder"] = "\U0001F4C1 Folder",
                ["BtnScan"] = "\U0001F50D Scan",
                ["BtnHide"] = "\U0001F648 Hide",
                ["BtnSuperHide"] = "\U0001F512 Strong",
                ["BtnUnhide"] = "\U0001F441 Show",
                ["BtnRemove"] = "\u2716 Remove",
                ["BtnRefresh"] = "\U0001F504 Refresh",
                ["SearchHint"] = "\U0001F50D Search files or folders...",
                ["EmptyTitle"] = "Empty List",
                ["EmptyDesc"] = "Add files/folders or use the scan button",
                ["StatusReady"] = "\u2705 Ready",
                ["StatusItemCount"] = "{0} items",
                ["SettingsTitle"] = "Settings",
                ["SettingsShortcuts"] = "\U0001F4CC Shortcuts",
                ["ShortcutSelectAll"] = "Ctrl+A : Select All",
                ["ShortcutHide"] = "Ctrl+H : Hide",
                ["ShortcutSuperHide"] = "Ctrl+Shift+H : Strong Hide",
                ["ShortcutUnhide"] = "Ctrl+U : Show",
                ["ShortcutDelete"] = "Delete : Remove from list",
                ["ShortcutSave"] = "Ctrl+S : Save list",
                ["ShortcutOpen"] = "Ctrl+O : Load list",
                ["ShortcutRefresh"] = "F5 : Refresh",
                ["SettingsSaveLoad"] = "\U0001F4BE Save & Load",
                ["SettingsSaveDesc1"] = "Item list is auto-saved to",
                ["SettingsSaveDesc2"] = "items.json file",
                ["SettingsAbout"] = "\u2139\uFE0F About",
                ["SettingsTagline"] = "Smart File Hiding",
                ["SettingsVersion"] = "Version 1.0",
                ["SettingsHideMethod"] = "Hiding with Hidden + System",
                ["SettingsFramework"] = ".NET 8 WPF",
                ["SettingsFooter"] = "Made with \u2764\uFE0F",
                ["SettingsLanguage"] = "\U0001F310 Language",
                ["ToastSelectedAll"] = "\u2705 All selected",
                ["ToastSaved"] = "\U0001F4BE List saved",
                ["ToastLoaded"] = "\U0001F4C2 List loaded",
                ["ToastNoSelection"] = "\u26A0\uFE0F No item selected",
                ["ToastScanFound"] = "\U0001F50D {0} hidden files found",
                ["ToastScanNone"] = "\U0001F50D No hidden files found",
                ["ToastAdded"] = "\u2705 {0} items added",
                ["ToastHideSuccess"] = "{0} items hidden",
                ["ToastSuperHideSuccess"] = "{0} items strongly hidden",
                ["ToastUnhideSuccess"] = "{0} items revealed",
                ["ToastRemoved"] = "\u2705 {0} items removed",
                ["ToastRefreshed"] = "\U0001F504 Status refreshed",
                ["ToastErrors"] = "{0} errors",
                ["ToastHideAction"] = "hidden",
                ["ToastSuperHideAction"] = "strongly hidden",
                ["ToastUnhideAction"] = "revealed",
                ["SettingsActions"] = "\u2699\uFE0F Actions",
                ["BtnClearAll"] = "\u26D4 Clear All",
                ["BtnExportList"] = "\U0001F4E4 Export List",
                ["BtnSelectAll"] = "\u2705 Select All",
                ["SettingsTips"] = "\U0001F4A1 Tips",
                ["TipMulti"] = "Ctrl+Click for multi-select",
                ["TipSuper"] = "Super Hide = Hidden + System attributes (bypasses \\\"Show hidden\\\")",
                ["TipDrag"] = "Drag & drop files from Explorer",
                ["TipScan"] = "Use Scan to find already-hidden files",
                ["SettingsStats"] = "\U0001F4CA Statistics",
                ["StatVisibleCount"] = "Visible: {0}",
                ["StatHiddenCount"] = "Hidden: {0}",
                ["StatTotalCount"] = "Total: {0}",
                ["ToastCleared"] = "\u2705 List cleared",
                ["ToastExported"] = "\U0001F4E4 List exported to desktop",
                ["MadeBy"] = "Made by mrlurix",
            },
            ["fa"] = new()
            {
                ["HeaderText"] = "Umbra \u2014 \u0645\u062F\u06CC\u0631\u06CC\u062A \u0641\u0627\u06CC\u0644\u200C\u0647\u0627 \u0648 \u067E\u0648\u0634\u0647\u200C\u0647\u0627",
                ["StatItems"] = "\u0622\u06CC\u062A\u0645\u200C\u0647\u0627",
                ["StatHidden"] = "\u0645\u062E\u0641\u06CC",
                ["StatTotalSize"] = "\u062D\u062C\u0645 \u06A9\u0644",
                ["StatusHidden"] = "\u0645\u062E\u0641\u06CC",
                ["StatusVisible"] = "\u0622\u0634\u06A9\u0627\u0631",
                ["BtnAddFile"] = "\u2795 \u0641\u0627\u06CC\u0644",
                ["BtnAddFolder"] = "\U0001F4C1 \u067E\u0648\u0634\u0647",
                ["BtnScan"] = "\U0001F50D \u0627\u0633\u06A9\u0646",
                ["BtnHide"] = "\U0001F648 \u0645\u062E\u0641\u06CC",
                ["BtnSuperHide"] = "\U0001F512 \u0642\u0648\u06CC",
                ["BtnUnhide"] = "\U0001F441 \u0622\u0634\u06A9\u0627\u0631",
                ["BtnRemove"] = "\u2716 \u062D\u0630\u0641",
                ["BtnRefresh"] = "\U0001F504 \u062A\u0627\u0632\u0647",
                ["SearchHint"] = "\U0001F50D \u062C\u0633\u062A\u062C\u0648\u06CC \u0641\u0627\u06CC\u0644 \u06CC\u0627 \u067E\u0648\u0634\u0647...",
                ["EmptyTitle"] = "\u0644\u06CC\u0633\u062A \u062E\u0627\u0644\u06CC",
                ["EmptyDesc"] = "\u0641\u0627\u06CC\u0644 \u06CC\u0627 \u067E\u0648\u0634\u0647 \u0627\u0636\u0627\u0641\u0647 \u06A9\u0646\u06CC\u062F \u06CC\u0627 \u0627\u0632 \u062F\u06A9\u0645\u0647 \u0627\u0633\u06A9\u0646 \u0627\u0633\u062A\u0641\u0627\u062F\u0647 \u06A9\u0646\u06CC\u062F",
                ["StatusReady"] = "\u2705 \u0622\u0645\u0627\u062F\u0647",
                ["StatusItemCount"] = "{0} \u0622\u06CC\u062A\u0645",
                ["SettingsTitle"] = "\u062A\u0646\u0638\u06CC\u0645\u0627\u062A",
                ["SettingsShortcuts"] = "\U0001F4CC \u06A9\u0644\u06CC\u062F\u0647\u0627\u06CC \u0645\u06CC\u0627\u0646\u0628\u0631",
                ["ShortcutSelectAll"] = "Ctrl+A : \u0627\u0646\u062A\u062E\u0627\u0628 \u0647\u0645\u0647",
                ["ShortcutHide"] = "Ctrl+H : \u0645\u062E\u0641\u06CC \u06A9\u0631\u062F\u0646",
                ["ShortcutSuperHide"] = "Ctrl+Shift+H : \u0645\u062E\u0641\u06CC\u200C\u0633\u0627\u0632\u06CC \u0642\u0648\u06CC",
                ["ShortcutUnhide"] = "Ctrl+U : \u0622\u0634\u06A9\u0627\u0631 \u06A9\u0631\u062F\u0646",
                ["ShortcutDelete"] = "Delete : \u062D\u0630\u0641 \u0627\u0632 \u0644\u06CC\u0633\u062A",
                ["ShortcutSave"] = "Ctrl+S : \u0630\u062E\u06CC\u0631\u0647 \u0644\u06CC\u0633\u062A",
                ["ShortcutOpen"] = "Ctrl+O : \u0628\u0627\u0632 \u06A9\u0631\u062F\u0646 \u0644\u06CC\u0633\u062A",
                ["ShortcutRefresh"] = "F5 : \u0628\u0631\u0648\u0632\u0631\u0633\u0627\u0646\u06CC",
                ["SettingsSaveLoad"] = "\U0001F4BE \u0630\u062E\u06CC\u0631\u0647 \u0648 \u0628\u0627\u0631\u06AF\u0630\u0627\u0631\u06CC",
                ["SettingsSaveDesc1"] = "\u0644\u06CC\u0633\u062A \u0622\u06CC\u062A\u0645\u200C\u0647\u0627 \u0628\u0647 \u0635\u0648\u0631\u062A \u062E\u0648\u062F\u06A9\u0627\u0631",
                ["SettingsSaveDesc2"] = "\u062F\u0631 \u0641\u0627\u06CC\u0644 items.json \u0630\u062E\u06CC\u0631\u0647 \u0645\u06CC\u200C\u0634\u0648\u062F",
                ["SettingsAbout"] = "\u2139\uFE0F \u062F\u0631\u0628\u0627\u0631\u0647",
                ["SettingsTagline"] = "\u0645\u062E\u0641\u06CC\u200C\u0633\u0627\u0632\u06CC \u0647\u0648\u0634\u0645\u0646\u062F \u0641\u0627\u06CC\u0644\u200C\u0647\u0627",
                ["SettingsVersion"] = "\u0646\u0633\u062E\u0647 \u06F1.\u06F0",
                ["SettingsHideMethod"] = "\u0645\u062E\u0641\u06CC\u200C\u0633\u0627\u0632\u06CC \u0628\u0627 Hidden + System",
                ["SettingsFramework"] = ".NET 8 WPF",
                ["SettingsFooter"] = "\u0637\u0631\u0627\u062D\u06CC \u0634\u062F\u0647 \u0628\u0627 \u2764\uFE0F",
                ["SettingsLanguage"] = "\U0001F310 \u0632\u0628\u0627\u0646",
                ["ToastSelectedAll"] = "\u2705 \u0647\u0645\u0647 \u0627\u0646\u062A\u062E\u0627\u0628 \u0634\u062F\u0646\u062F",
                ["ToastSaved"] = "\U0001F4BE \u0644\u06CC\u0633\u062A \u0630\u062E\u06CC\u0631\u0647 \u0634\u062F",
                ["ToastLoaded"] = "\U0001F4C2 \u0644\u06CC\u0633\u062A \u0628\u0627\u0631\u06AF\u0630\u0627\u0631\u06CC \u0634\u062F",
                ["ToastNoSelection"] = "\u26A0\uFE0F \u0622\u06CC\u062A\u0645\u06CC \u0627\u0646\u062A\u062E\u0627\u0628 \u0646\u0634\u062F\u0647",
                ["ToastScanFound"] = "\U0001F50D {0} \u0641\u0627\u06CC\u0644 \u0645\u062E\u0641\u06CC \u067E\u06CC\u062F\u0627 \u0634\u062F",
                ["ToastScanNone"] = "\U0001F50D \u0641\u0627\u06CC\u0644 \u0645\u062E\u0641\u06CC\u200C\u0627\u06CC \u067E\u06CC\u062F\u0627 \u0646\u0634\u062F",
                ["ToastAdded"] = "\u2705 {0} \u0622\u06CC\u062A\u0645 \u0627\u0636\u0627\u0641\u0647 \u0634\u062F",
                ["ToastHideSuccess"] = "{0} \u0622\u06CC\u062A\u0645 \u0645\u062E\u0641\u06CC \u0634\u062F",
                ["ToastSuperHideSuccess"] = "{0} \u0622\u06CC\u062A\u0645 \u0645\u062E\u0641\u06CC\u200C\u0633\u0627\u0632\u06CC \u0642\u0648\u06CC \u0634\u062F",
                ["ToastUnhideSuccess"] = "{0} \u0622\u06CC\u062A\u0645 \u0622\u0634\u06A9\u0627\u0631 \u0634\u062F",
                ["ToastRemoved"] = "\u2705 {0} \u0622\u06CC\u062A\u0645 \u062D\u0630\u0641 \u0634\u062F",
                ["ToastRefreshed"] = "\U0001F504 \u0648\u0636\u0639\u06CC\u062A \u0628\u0631\u0648\u0632\u0631\u0633\u0627\u0646\u06CC \u0634\u062F",
                ["ToastErrors"] = "{0} \u062E\u0637\u0627",
                ["ToastHideAction"] = "\u0645\u062E\u0641\u06CC \u0634\u062F",
                ["ToastSuperHideAction"] = "\u0645\u062E\u0641\u06CC\u200C\u0633\u0627\u0632\u06CC \u0642\u0648\u06CC \u0634\u062F",
                ["ToastUnhideAction"] = "\u0622\u0634\u06A9\u0627\u0631 \u0634\u062F",
                ["SettingsActions"] = "\u2699\uFE0F \u0639\u0645\u0644\u06CC\u0627\u062A",
                ["BtnClearAll"] = "\u26D4 \u067E\u0627\u06A9 \u06A9\u0631\u062F\u0646 \u0647\u0645\u0647",
                ["BtnExportList"] = "\U0001F4E4 \u062E\u0631\u0648\u062C\u06CC \u0644\u06CC\u0633\u062A",
                ["BtnSelectAll"] = "\u2705 \u0627\u0646\u062A\u062E\u0627\u0628 \u0647\u0645\u0647",
                ["SettingsTips"] = "\U0001F4A1 \u0646\u06A9\u0627\u062A",
                ["TipMulti"] = "\u0627\u0646\u062A\u062E\u0627\u0628 \u0686\u0646\u062F\u062A\u0627\u06CC\u06CC \u0628\u0627 Ctrl+Click",
                ["TipSuper"] = "\u0645\u062E\u0641\u06CC\u200C\u0633\u0627\u0632\u06CC \u0642\u0648\u06CC = Hidden + System",
                ["TipDrag"] = "\u06A9\u0634\u06CC\u062F\u0646 \u0648 \u0631\u0647\u0627 \u06A9\u0631\u062F\u0646 \u0641\u0627\u06CC\u0644 \u0627\u0632 Explorer",
                ["TipScan"] = "\u0628\u0627 \u0627\u0633\u06A9\u0646 \u0641\u0627\u06CC\u0644\u200C\u0647\u0627\u06CC \u0627\u0632 \u0642\u0628\u0644 \u0645\u062E\u0641\u06CC \u0631\u0648 \u067E\u06CC\u062F\u0627 \u06A9\u0646\u06CC\u062F",
                ["SettingsStats"] = "\U0001F4CA \u0622\u0645\u0627\u0631",
                ["StatVisibleCount"] = "\u0622\u0634\u06A9\u0627\u0631: {0}",
                ["StatHiddenCount"] = "\u0645\u062E\u0641\u06CC: {0}",
                ["StatTotalCount"] = "\u0645\u062C\u0645\u0648\u0639: {0}",
                ["ToastCleared"] = "\u2705 \u0644\u06CC\u0633\u062A \u067E\u0627\u06A9 \u0634\u062F",
                ["ToastExported"] = "\U0001F4E4 \u0644\u06CC\u0633\u062A \u0631\u0648\u06CC \u062F\u0633\u06A9\u062A\u0627\u067E \u0630\u062E\u06CC\u0631\u0647 \u0634\u062F",
                ["MadeBy"] = "\u0633\u0627\u062E\u062A\u0647 \u0634\u062F\u0647 \u062A\u0648\u0633\u0637 mrlurix",
            }
        };

        public string T(string key) => Data[_lang].GetValueOrDefault(key, Data["en"].GetValueOrDefault(key, key));

        // Shortcut properties for XAML bindings
        public string HeaderText => T("HeaderText");
        public string StatItems => T("StatItems");
        public string StatHidden => T("StatHidden");
        public string StatTotalSize => T("StatTotalSize");
        public string StatusHidden => T("StatusHidden");
        public string StatusVisible => T("StatusVisible");
        public string BtnAddFile => T("BtnAddFile");
        public string BtnAddFolder => T("BtnAddFolder");
        public string BtnScan => T("BtnScan");
        public string BtnHide => T("BtnHide");
        public string BtnSuperHide => T("BtnSuperHide");
        public string BtnUnhide => T("BtnUnhide");
        public string BtnRemove => T("BtnRemove");
        public string BtnRefresh => T("BtnRefresh");
        public string SearchHint => T("SearchHint");
        public string EmptyTitle => T("EmptyTitle");
        public string EmptyDesc => T("EmptyDesc");
        public string StatusReady => T("StatusReady");
        public string StatusItemCount => T("StatusItemCount");
        public string SettingsTitle => T("SettingsTitle");
        public string SettingsShortcuts => T("SettingsShortcuts");
        public string ShortcutSelectAll => T("ShortcutSelectAll");
        public string ShortcutHide => T("ShortcutHide");
        public string ShortcutSuperHide => T("ShortcutSuperHide");
        public string ShortcutUnhide => T("ShortcutUnhide");
        public string ShortcutDelete => T("ShortcutDelete");
        public string ShortcutSave => T("ShortcutSave");
        public string ShortcutOpen => T("ShortcutOpen");
        public string ShortcutRefresh => T("ShortcutRefresh");
        public string SettingsSaveLoad => T("SettingsSaveLoad");
        public string SettingsSaveDesc1 => T("SettingsSaveDesc1");
        public string SettingsSaveDesc2 => T("SettingsSaveDesc2");
        public string SettingsAbout => T("SettingsAbout");
        public string SettingsTagline => T("SettingsTagline");
        public string SettingsVersion => T("SettingsVersion");
        public string SettingsHideMethod => T("SettingsHideMethod");
        public string SettingsFramework => T("SettingsFramework");
        public string SettingsFooter => T("SettingsFooter");
        public string SettingsLanguage => T("SettingsLanguage");
        public string ToastSelectedAll => T("ToastSelectedAll");
        public string ToastSaved => T("ToastSaved");
        public string ToastLoaded => T("ToastLoaded");
        public string ToastNoSelection => T("ToastNoSelection");
        public string ToastScanFound => T("ToastScanFound");
        public string ToastScanNone => T("ToastScanNone");
        public string ToastAdded => T("ToastAdded");
        public string ToastHideSuccess => T("ToastHideSuccess");
        public string ToastSuperHideSuccess => T("ToastSuperHideSuccess");
        public string ToastUnhideSuccess => T("ToastUnhideSuccess");
        public string ToastRemoved => T("ToastRemoved");
        public string ToastRefreshed => T("ToastRefreshed");
        public string ToastErrors => T("ToastErrors");
        public string ToastHideAction => T("ToastHideAction");
        public string ToastSuperHideAction => T("ToastSuperHideAction");
        public string SettingsActions => T("SettingsActions");
        public string BtnClearAll => T("BtnClearAll");
        public string BtnExportList => T("BtnExportList");
        public string BtnSelectAll => T("BtnSelectAll");
        public string SettingsTips => T("SettingsTips");
        public string TipMulti => T("TipMulti");
        public string TipSuper => T("TipSuper");
        public string TipDrag => T("TipDrag");
        public string TipScan => T("TipScan");
        public string SettingsStats => T("SettingsStats");
        public string StatVisibleCount => T("StatVisibleCount");
        public string StatHiddenCount => T("StatHiddenCount");
        public string StatTotalCount => T("StatTotalCount");
        public string ToastCleared => T("ToastCleared");
        public string ToastExported => T("ToastExported");
        public string MadeBy => T("MadeBy");
        public string ToastUnhideAction => T("ToastUnhideAction");

        public static event Action? LanguageChanged;

        public void NotifyAll()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
            LanguageChanged?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

