using System.Configuration;
using System.Data;
using System.Windows;
using CommunicationApp.Views;

namespace CommunicationApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected void Application_Startup(object sender, StartupEventArgs e)
    {
        var loginWindow = new LoginWindow();
        if (loginWindow.ShowDialog() == true && !string.IsNullOrEmpty(loginWindow.Username))
        {
            var mainWindow = new MainChatWindow(loginWindow.Username);
            mainWindow.Show();
        }
        else
        {
            Shutdown();
        }
    }
}

