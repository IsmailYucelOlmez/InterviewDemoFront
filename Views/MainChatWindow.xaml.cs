using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunicationApp.Models;
using CommunicationApp.Services;
using Microsoft.Win32;

namespace CommunicationApp.Views;

public partial class MainChatWindow : Window
{
    private readonly SignalRService _signalRService;
    private readonly string _currentUsername;
    private string? _selectedUser;
    private readonly ObservableCollection<User> _users;
    private readonly ObservableCollection<MessageDisplay> _messages;

    public MainChatWindow(string username)
    {
        InitializeComponent();
        _currentUsername = username;
        _signalRService = new SignalRService();
        _users = new ObservableCollection<User>();
        _messages = new ObservableCollection<MessageDisplay>();

        UsersListBox.ItemsSource = _users;
        MessagesItemsControl.ItemsSource = _messages;

        InitializeSignalR();
    }

    private async void InitializeSignalR()
    {
        try
        {
            _signalRService.MessageReceived += OnMessageReceived;
            _signalRService.UserStatusChanged += OnUserStatusChanged;
            _signalRService.SystemMessageReceived += OnSystemMessageReceived;
            _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;

            await _signalRService.ConnectAsync(_currentUsername);
            await LoadOnlineUsers();
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

    private void OnMessageReceived(Message message)
    {
        Dispatcher.Invoke(() =>
        {
            var display = new MessageDisplay
            {
                From = message.From,
                MessageText = message.MessageText,
                Timestamp = message.Timestamp,
                IsFromMe = message.From == _currentUsername
            };
            _messages.Add(display);
            ScrollToBottom();
        });
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

    private void OnSystemMessageReceived(string message)
    {
        Dispatcher.Invoke(() =>
        {
            SystemMessageTextBlock.Text = message;
            var border = (Border)SystemMessageTextBlock.Parent;
            border.Visibility = Visibility.Visible;
            
            Task.Delay(5000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() => border.Visibility = Visibility.Collapsed);
            });
        });
    }

    private void OnConnectionStatusChanged(string status)
    {
        Dispatcher.Invoke(() =>
        {
            Title = $"İletişim Uygulaması - {status}";
        });
    }

    private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersListBox.SelectedItem is User selectedUser)
        {
            _selectedUser = selectedUser.Username;
            SelectedUserTextBlock.Text = $"Konuşma: {_selectedUser}";
            _messages.Clear();
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessage();
    }

    private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            await SendMessage();
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_selectedUser))
        {
            MessageBox.Show("Lütfen bir kullanıcı seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var messageText = MessageTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(messageText))
            return;

        try
        {
            await _signalRService.SendMessageAsync(_currentUsername, _selectedUser, messageText);
            MessageTextBox.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Mesaj gönderilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SendFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedUser))
        {
            MessageBox.Show("Lütfen bir kullanıcı seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Gönderilecek Dosyayı Seçin",
            Filter = "Tüm Dosyalar|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var fileData = await File.ReadAllBytesAsync(dialog.FileName);
                var fileName = Path.GetFileName(dialog.FileName);

                var emailDialog = new EmailInputDialog();
                if (emailDialog.ShowDialog() == true)
                {
                    var email = emailDialog.Email;
                    await _signalRService.SendFileAsync(_selectedUser, fileName, fileData, email);
                    MessageBox.Show("Dosya gönderildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosya gönderilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    private async void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await _signalRService.DisconnectAsync();
        Close();
    }

    private void ScrollToBottom()
    {
        MessagesScrollViewer.ScrollToEnd();
    }

    protected override async void OnClosed(EventArgs e)
    {
        await _signalRService.DisconnectAsync();
        base.OnClosed(e);
    }
}

public class MessageDisplay
{
    public string From { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsFromMe { get; set; }
}

