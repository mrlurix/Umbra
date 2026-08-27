using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Umbra
{
    internal static class SecurityHelper
    {
        private const int MaxPathLength = 32767;
        private const int MaxItemsCount = 5000;
        private const long MaxJsonSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const long MaxSingleFileSize = 10L * 1024 * 1024 * 1024; // 10 GB sanity cap

        private static readonly HashSet<string> CriticalPaths = new string[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Boot"),
        }.Where(p => !string.IsNullOrEmpty(p)).Select(p => NormalizeForCompare(p)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] CriticalRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\"
        }.Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        private static string NormalizeForCompare(string p)
        {
            try { return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant(); }
            catch { return p.TrimEnd('\\', '/').ToUpperInvariant(); }
        }

        public static bool IsValidPath(string? path, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "مسیر خالی است";
                return false;
            }
            if (path.Length > MaxPathLength)
            {
                error = "مسیر خیلی طولانی است";
                return false;
            }
            if (path.IndexOf('\0') >= 0)
            {
                error = "مسیر نامعتبر است";
                return false;
            }
            // Block NT device paths, UNC device, ADS
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
                path.StartsWith(@"\\.\", StringComparison.Ordinal) ||
                path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                error = "مسیر device پشتیبانی نمی‌شود";
                return false;
            }
            // ADS (alternate data stream) contains colon after drive letter
            // Allow C:\ but block C:\file.txt:stream
            var afterRoot = path.Length > 2 ? path.Substring(2) : "";
            if (afterRoot.Contains(':'))
            {
                error = "Alternate Data Stream مجاز نیست";
                return false;
            }
            try
            {
                var invalid = Path.GetInvalidPathChars();
                if (path.IndexOfAny(invalid) >= 0)
                {
                    error = "مسیر حاوی کاراکتر نامعتبر است";
                    return false;
                }
            }
            catch { }
            return true;
        }

        public static bool TryCanonicalize(string path, out string canonical, out string error)
        {
            canonical = "";
            error = "";
            if (!IsValidPath(path, out error)) return false;
            try
            {
                canonical = Path.GetFullPath(path);
                // Also reject if canonical still has invalid chars
                if (!IsValidPath(canonical, out error)) return false;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool IsCriticalSystemPath(string canonicalPath)
        {
            if (string.IsNullOrWhiteSpace(canonicalPath)) return true;
            var norm = NormalizeForCompare(canonicalPath);
            if (CriticalPaths.Contains(norm)) return true;

            // Block drive roots like C:\, D:\
            if (norm.Length == 2 && norm[1] == ':' ) return true; // C:
            if (norm.Length == 3 && norm[1] == ':' && (norm[2] == '\\' || norm[2] == '/')) return true; // C:\

            // Block Windows root itself
            foreach (var crit in CriticalPaths)
            {
                if (norm.Equals(crit, StringComparison.OrdinalIgnoreCase)) return true;
            }
            // Also block exact Windows folder
            var winDir = NormalizeForCompare(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            if (!string.IsNullOrEmpty(winDir) && norm.Equals(winDir, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        public static bool IsSafeToHide(string canonicalPath, out string reason)
        {
            reason = "";
            if (IsCriticalSystemPath(canonicalPath))
            {
                reason = "مسیر سیستمی حساس است و نمی‌توان آن را مخفی کرد";
                return false;
            }
            // Block if path is directly under Windows (extra safety)
            try
            {
                var winDir = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)).TrimEnd('\\') + "\\";
                if (canonicalPath.StartsWith(winDir, StringComparison.OrdinalIgnoreCase))
                {
                    // Allow subfolders? Be conservative: block System32, SysWOW64, WinSxS
                    var lower = canonicalPath.ToLowerInvariant();
                    if (lower.Contains("\\system32") || lower.Contains("\\syswow64") || lower.Contains("\\winsxs") || lower.Contains("\\boot"))
                    {
                        reason = "مسیر حساس ویندوز است";
                        return false;
                    }
                }
            }
            catch { }
            return true;
        }

        public static bool IsReparsePoint(string path)
        {
            try
            {
                var attr = File.GetAttributes(path);
                return (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }
            catch { return false; }
        }

        public static bool ValidateItemData(ItemData d, out string error)
        {
            error = "";
            if (d == null) { error = "آیتم null است"; return false; }
            if (string.IsNullOrWhiteSpace(d.FullPath)) { error = "مسیر خالی است"; return false; }
            if (d.Size < 0 || d.Size > MaxSingleFileSize) { error = "حجم نامعتبر"; return false; }
            if (!IsValidPath(d.FullPath, out error)) return false;
            if (!TryCanonicalize(d.FullPath, out var canon, out error)) return false;
            // Not checking IsCritical here for load - just validate, blocking happens on hide
            if (canon.Length > MaxPathLength) { error = "مسیر طولانی"; return false; }
            return true;
        }

        public static bool ValidateJsonSize(string json, out string error)
        {
            error = "";
            if (json.Length > MaxJsonSizeBytes) { error = "فایل داده خیلی بزرگ است"; return false; }
            return true;
        }

        public static bool ValidateItemsCount(int count, out string error)
        {
            error = "";
            if (count > MaxItemsCount) { error = $"تعداد آیتم‌ها نمی‌تواند بیشتر از {MaxItemsCount} باشد"; return false; }
            return true;
        }

        public const int MaxItems = MaxItemsCount;
        public const long MaxJsonSize = MaxJsonSizeBytes;
    }
}
