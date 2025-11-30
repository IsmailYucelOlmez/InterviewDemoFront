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
        RegisterButton.IsEnabled = false;
        StatusTextBlock.Visibility = Visibility.Visible;
        StatusTextBlock.Text = "Giriş yapılıyor...";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

        try
        {
            var result = await _authService.LoginAsync(username, password);
            if (result.Success)
            {
                Username = username;
                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus(result.ErrorMessage ?? "Giriş başarısız! Kullanıcı adı veya şifre hatalı.");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Bağlantı hatası: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            RegisterButton.IsEnabled = true;
        }
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        // Email alanını göster
        if (EmailLabel.Visibility != Visibility.Visible)
        {
            EmailLabel.Visibility = Visibility.Visible;
            EmailTextBox.Visibility = Visibility.Visible;
            return;
        }

        var username = UsernameTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Kullanıcı adı, e-posta ve şifre boş olamaz!");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Geçerli bir e-posta adresi giriniz!");
            return;
        }

        LoginButton.IsEnabled = false;
        RegisterButton.IsEnabled = false;
        StatusTextBlock.Visibility = Visibility.Visible;
        StatusTextBlock.Text = "Kayıt yapılıyor...";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

        try
        {
            var result = await _authService.RegisterAsync(username, email, password);
            if (result.Success)
            {
                ShowStatus("Kayıt başarılı! Giriş yapabilirsiniz.", System.Windows.Media.Brushes.Green);
                // Email alanını tekrar gizle
                EmailLabel.Visibility = Visibility.Collapsed;
                EmailTextBox.Visibility = Visibility.Collapsed;
                EmailTextBox.Text = string.Empty;
            }
            else
            {
                ShowStatus(result.ErrorMessage ?? "Kayıt başarısız! Kullanıcı adı veya e-posta zaten kullanılıyor olabilir.");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Bağlantı hatası: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            RegisterButton.IsEnabled = true;
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    private void ShowStatus(string message, System.Windows.Media.Brush? foreground = null)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = foreground ?? System.Windows.Media.Brushes.Red;
        StatusTextBlock.Visibility = Visibility.Visible;
    }
}

