using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Umbra
{
    public partial class App : Application
    {
        private const long MaxLogSizeBytes = 2 * 1024 * 1024; // 2 MB
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Umbra", "Umbra_error.log");

        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("DispatcherUnhandledException", e.Exception);
            // FIX: Don't leak stack trace to user (information disclosure)
            MessageBox.Show("خطای غیرمنتظره رخ داد. جزئیات در فایل لاگ ذخیره شد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogError("UnhandledException", ex);
        }

        private void LogError(string source, Exception ex)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                // FIX: Prevent unbounded log growth (disk fill DoS)
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogSizeBytes)
                {
                    // Rotate: keep last 50% and truncate
                    var text = File.ReadAllText(LogPath);
                    var keep = text.Substring(text.Length / 2);
                    File.WriteAllText(LogPath, keep);
                }

                // FIX: Don't log full stack trace with internal paths in production? Minimal info
                string log = $"=== {source} ===\nTime: {DateTime.Now:O}\nType: {ex.GetType().Name}\nMessage: {ex.Message}\n\n";
                File.AppendAllText(LogPath, log);
            }
            catch { }
        }
    }
}
