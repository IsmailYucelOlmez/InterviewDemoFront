using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    private readonly ChatService _chatService;
    private readonly string _currentUsername;
    private string? _selectedUser;
    private readonly ObservableCollection<User> _users;
    private readonly ObservableCollection<MessageDisplay> _messages;

    public MainChatWindow(string username)
    {
        try
        {
            InitializeComponent();
            _currentUsername = username ?? throw new ArgumentNullException(nameof(username));
            
            try
            {
                _signalRService = new SignalRService();
                _chatService = new ChatService();
            }
            catch (Exception ex)
            {
                throw new Exception($"Servisler başlatılamadı: {ex.Message}", ex);
            }
            
            _users = new ObservableCollection<User>();
            _messages = new ObservableCollection<MessageDisplay>();

            UsersListBox.ItemsSource = _users;
            MessagesItemsControl.ItemsSource = _messages;

            // InitializeSignalR async olduğu için exception'ları kendi içinde yakalıyor
            InitializeSignalR();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Chat penceresi açılırken hata oluştu:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nİç Hata: {ex.InnerException.Message}";
            }
            errorMessage += $"\n\nHata Tipi: {ex.GetType().Name}";
            
            MessageBox.Show(errorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            throw; // Exception'ı yukarı fırlat ki App.xaml.cs yakalayabilsin
        }
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

    private async Task LoadMessageHistory(string otherUser)
    {
        try
        {
            // Önce SignalR GetMessageHistory metodunu dene (iki kullanıcı arası mesajlar)
            var messages = await _signalRService.GetMessageHistoryAsync(_currentUsername, otherUser);
            
            // Eğer mesaj yoksa GetUserMessages ile dene
            if (messages == null || messages.Count == 0)
            {
                // Kullanıcının tüm mesajlarını al ve filtrele
                var allMessages = await _signalRService.GetUserMessagesAsync(_currentUsername);
                messages = allMessages.Where(m => 
                    (m.From == _currentUsername && m.To == otherUser) ||
                    (m.From == otherUser && m.To == _currentUsername)
                ).ToList();
            }
            
            _messages.Clear();
            
            foreach (var message in messages.OrderBy(m => m.Timestamp))
            {
                var display = new MessageDisplay
                {
                    From = message.From,
                    MessageText = message.Message,
                    Timestamp = message.Timestamp,
                    IsFromMe = message.From == _currentUsername
                };
                _messages.Add(display);
            }
            
            ScrollToBottom();
        }
        catch
        {
            // SignalR başarısız olursa REST API'yi dene (fallback)
            try
            {
                var messages = await _chatService.GetMessageHistoryAsync(_currentUsername, otherUser);
                _messages.Clear();
                
                foreach (var message in messages.OrderBy(m => m.Timestamp))
                {
                    var display = new MessageDisplay
                    {
                        From = message.From,
                        MessageText = message.Message,
                        Timestamp = message.Timestamp,
                        IsFromMe = message.From == _currentUsername
                    };
                    _messages.Add(display);
                }
                
                ScrollToBottom();
            }
            catch
            {
                // Her iki yöntem de başarısız olursa sessizce devam et
            }
        }
    }

    private void OnMessageReceived(ChatMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            // Sadece seçili kullanıcıyla olan mesajları göster
            if (string.IsNullOrWhiteSpace(_selectedUser))
                return;

            // Mesaj seçili kullanıcıyla ilgili mi kontrol et
            bool isRelevantMessage = (message.From == _selectedUser && message.To == _currentUsername) ||
                                     (message.From == _currentUsername && message.To == _selectedUser);

            if (!isRelevantMessage)
                return;

            // Aynı mesaj zaten listede var mı kontrol et (optimistic update ile eklenmiş olabilir)
            // Timestamp ve mesaj içeriğine göre kontrol et
            var existingMessage = _messages.FirstOrDefault(m => 
                m.From == message.From && 
                m.MessageText == message.Message && 
                Math.Abs((m.Timestamp - message.Timestamp).TotalSeconds) < 2); // 2 saniye tolerans

            if (existingMessage != null)
            {
                // Mesaj zaten var, sadece timestamp'i güncelle
                existingMessage.Timestamp = message.Timestamp;
                return;
            }

            var display = new MessageDisplay
            {
                From = message.From,
                MessageText = message.Message,
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

    private async void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersListBox.SelectedItem is User selectedUser)
        {
            _selectedUser = selectedUser.Username;
            SelectedUserTextBlock.Text = $"Konuşma: {_selectedUser}";
            _messages.Clear();
            
            // Seçili kullanıcıyla olan mesaj geçmişini yükle
            await LoadMessageHistory(_selectedUser);
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

        // Mesajı hemen UI'a ekle (optimistic update)
        var display = new MessageDisplay
        {
            From = _currentUsername,
            MessageText = messageText,
            Timestamp = DateTime.Now,
            IsFromMe = true
        };
        _messages.Add(display);
        ScrollToBottom();
        MessageTextBox.Clear();

        try
        {
            await _signalRService.SendMessageAsync(_currentUsername, _selectedUser, messageText);
        }
        catch (Exception ex)
        {
            // Hata durumunda mesajı listeden kaldır
            _messages.Remove(display);
            MessageBox.Show($"Mesaj gönderilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFileEmailWindow_Click(object sender, RoutedEventArgs e)
    {
        // Dosyayı mail ile göndermek için ayrı sayfayı aç
        var window = new FileEmailWindow(_currentUsername);
        window.Owner = this;
        window.Show();
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

