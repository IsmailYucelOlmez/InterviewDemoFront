using System.Windows;
using CommunicationApp.Services;
using CommunicationApp.Views;

namespace CommunicationApp.Views;

public partial class LoginWindow : Window
{
    private readonly AuthenticationService _authService;

    public string? Username { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        _authService = new AuthenticationService();
        UsernameTextBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Kullanıcı adı ve şifre boş olamaz!");
            return;
        }

        LoginButton.IsEnabled = false;
        StatusTextBlock.Visibility = Visibility.Visible;
        StatusTextBlock.Text = "Giriş yapılıyor...";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

        try
        {
            var success = await _authService.LoginAsync(username, password);
            if (success)
            {
                Username = username;
                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus("Giriş başarısız! Kullanıcı adı veya şifre hatalı.");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Bağlantı hatası: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    private void ShowStatus(string message)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        StatusTextBlock.Visibility = Visibility.Visible;
    }
}

