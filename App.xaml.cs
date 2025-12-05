using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunicationApp.Views;

namespace CommunicationApp;

public partial class App : Application
{
    protected void Application_Startup(object sender, StartupEventArgs e)
    {

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        
        try
        {
            var loginWindow = new LoginWindow();
            var loginResult = loginWindow.ShowDialog();
            
            if (loginResult == true && !string.IsNullOrEmpty(loginWindow.Username))
            {
                try
                {
                    var mainWindow = new MainChatWindow(loginWindow.Username);
                    MainWindow = mainWindow;
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    mainWindow.Show();
                }
                catch (Exception ex)
                {
                    LogException("MainChatWindow oluşturulurken", ex);
                    MessageBox.Show($"Chat penceresi açılırken hata oluştu:\n\n{ex.Message}\n\nDetay: {ex.GetType().Name}\n\nStack Trace:\n{ex.StackTrace}", 
                        "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                }
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            LogException("Application_Startup", ex);
            MessageBox.Show($"Uygulama başlatılırken hata oluştu:\n\n{ex.Message}\n\nDetay: {ex.GetType().Name}\n\nStack Trace:\n{ex.StackTrace}", 
                "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Unhandled Exception", e.Exception);
        
        var errorMessage = $"Yakalanmamış bir hata oluştu:\n\n{e.Exception.Message}\n\nHata Tipi: {e.Exception.GetType().Name}";
        if (e.Exception.InnerException != null)
        {
            errorMessage += $"\n\nİç Hata: {e.Exception.InnerException.Message}";
        }
        errorMessage += $"\n\nStack Trace:\n{e.Exception.StackTrace}";
        
        MessageBox.Show(errorMessage, "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        
        Shutdown();
    }

    private void LogException(string context, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n" +
                           $"Message: {ex.Message}\n" +
                           $"Type: {ex.GetType().Name}\n" +
                           $"Stack Trace: {ex.StackTrace}\n" +
                           $"{(ex.InnerException != null ? $"Inner Exception: {ex.InnerException.Message}\n" : "")}" +
                           $"{new string('-', 80)}\n\n";
            File.AppendAllText(logPath, logMessage);
        }
        catch
        {
            
        }
    }
}

