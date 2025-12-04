using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommunicationApp.Models;
using CommunicationApp.Services;
using Microsoft.Win32;

namespace CommunicationApp.Views;

public partial class FileEmailWindow : Window
{
    private readonly SignalRService _signalRService;
    private readonly MailService _mailService;
    private readonly string _currentUsername;

    private readonly ObservableCollection<User> _users;
    private string? _selectedUser;
    private string? _selectedUserEmail;

    private string? _selectedFilePath;

    public FileEmailWindow(string username, SignalRService signalRService)
    {
        InitializeComponent();

        _currentUsername = username ?? throw new ArgumentNullException(nameof(username));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _mailService = new MailService();

        _users = new ObservableCollection<User>();
        UsersListBox.ItemsSource = _users;

        InitializeSignalR();
    }

    private async void InitializeSignalR()
    {
        try
        {
            _signalRService.UserStatusChanged += OnUserStatusChanged;
            _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;

            if (_signalRService.IsConnected)
            {
                await LoadOnlineUsers();
            }
            else
            {
                await _signalRService.ConnectAsync(_currentUsername);
                await LoadOnlineUsers();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Bağlantı hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadOnlineUsers()
    {
        try
        {
            var users = await _signalRService.GetOnlineUsersAsync();
            _users.Clear();

            foreach (var user in users.Where(u => u.Username != _currentUsername))
            {
                _users.Add(user);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kullanıcılar yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnUserStatusChanged(string username, bool isOnline)
    {
        Dispatcher.Invoke(() =>
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                user.IsOnline = isOnline;
            }
            else if (isOnline && username != _currentUsername)
            {
                _users.Add(new User { Username = username, IsOnline = true });
            }
        });
    }

    private void OnConnectionStatusChanged(string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusTextBlock.Text = status;
            StatusTextBlock.Visibility = Visibility.Visible;
        });
    }

    private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersListBox.SelectedItem is User selectedUser)
        {
            _selectedUser = selectedUser.Username;
            _selectedUserEmail = selectedUser.Email;
            SelectedUserTextBlock.Text = $"Seçili kullanıcı: {_selectedUser}";
        }
    }

    private void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Gönderilecek Dosyayı Seçin",
            Filter = "Tüm Dosyalar|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedFilePath = dialog.FileName;
            SelectedFileTextBlock.Text = Path.GetFileName(_selectedFilePath);
        }
    }

    private async void SendEmailButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedUser))
        {
            MessageBox.Show("Lütfen soldan bir kullanıcı seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedUserEmail))
        {
            MessageBox.Show("Seçili kullanıcının e-posta adresi bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            MessageBox.Show("Lütfen gönderilecek dosyayı seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var fileData = await File.ReadAllBytesAsync(_selectedFilePath);
            var fileName = Path.GetFileName(_selectedFilePath);

            await _mailService.SendFileByMailAsync(_currentUsername, _selectedUser!, _selectedUserEmail!, fileName, fileData);

            StatusTextBlock.Text = "Dosya mail olarak gönderildi.";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            StatusTextBlock.Visibility = Visibility.Visible;

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dosya gönderilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _signalRService.UserStatusChanged -= OnUserStatusChanged;
        _signalRService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        base.OnClosed(e);
    }
}


