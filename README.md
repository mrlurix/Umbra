# 🌘 Umbra

Smart file & folder hiding manager for Windows — built with WPF and .NET 8

---

## ✨ Features

- 🔒 **Hide files & folders** via system attributes (Hidden)
- 🛡️ **Super Hide** with Hidden + System — files remain invisible even with “Show hidden files” enabled
- 🧲 **Drag & Drop** from Explorer
- 🔍 **Scan** folders to find already-hidden files
- 🗂️ **Search**, multi-select, remove from list and refresh status
- 💾 **Auto save/load** item list in `items.json`
- 📤 **Export list** to text file on Desktop
- 🌐 **English UI** (LTR) with clean modern theme
- 🎨 Dark theme with neon accents, smooth animations and custom icon
- 🧭 Full settings panel with stats, shortcuts, tips and about

## ⌨️ Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+A` | Select All |
| `Ctrl+H` | Hide |
| `Ctrl+Shift+H` | Strong Hide |
| `Ctrl+U` | Unhide |
| `Delete` | Remove from list |
| `Ctrl+S` | Save list |
| `Ctrl+O` | Load list |
| `F5` | Refresh status |

## 🛠️ Build

```bash
dotnet build -c Release
```

## 📦 Zero-prerequisite executable

Self-Contained Single-File — runs on any Windows 7+ without installing .NET:

```bash
# 64-bit (modern Windows)
dotnet publish -c Release -r win-x64 -o publish/win-x64

# 32-bit (compatible with all systems)
dotnet publish -c Release -r win-x86 -o publish/win-x86
```

Output: `publish/win-x64/Umbra.exe` (~68MB compressed) and `publish/win-x86/Umbra.exe` (~63MB) — no prerequisites required

## ⚙️ Tech

- WPF (Windows Presentation Foundation)
- .NET 8.0
- C#
- Font: Segoe UI

---

Made by **mrlurix** 🖤
