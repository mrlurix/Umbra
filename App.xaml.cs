using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Umbra
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("DispatcherUnhandledException", e.Exception);
            MessageBox.Show($"خطا: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
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
                string log = $"=== {source} ===\nTime: {DateTime.Now}\nMessage: {ex}\nStack: {ex.StackTrace}\n\n";
                File.AppendAllText("Umbra_error.log", log);
            }
            catch { }
        }
    }
}
