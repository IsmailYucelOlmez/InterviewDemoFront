using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApp.Helpers;
using CommunicationApp.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CommunicationApp.Services;

public class SignalRService : IDisposable
{
    private HubConnection? _connection;
    private readonly AppSettings _settings;

    public event Action<ChatMessage>? MessageReceived;
    public event Action<string, bool>? UserStatusChanged;
    public event Action<string>? SystemMessageReceived;
    public event Action<string>? ConnectionStatusChanged;

    public SignalRService()
    {
        _settings = ConfigHelper.GetSettings();
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string username)
    {
        try
        {
            var hubUrl = _settings.ServerSettings.HubUrl;
            
            // Hub URL'ini kontrol et ve düzelt
            if (string.IsNullOrWhiteSpace(hubUrl))
            {
                throw new Exception("Hub URL ayarlanmamış. Lütfen Ayarlar menüsünden Hub URL'ini kontrol edin.");
            }
            
            // URL'nin sonunda / varsa kaldır
            if (hubUrl.EndsWith("/"))
            {
                hubUrl = hubUrl.TrimEnd('/');
            }
            
            // Username'i query string olarak ekle
            var urlWithUsername = $"{hubUrl}?username={Uri.EscapeDataString(username)}";
            
            _connection = new HubConnectionBuilder()
                .WithUrl(urlWithUsername)
                .WithAutomaticReconnect() // Otomatik yeniden bağlanma
                .Build();

            _connection.On<ChatMessage>("ReceiveMessage", (message) =>
            {
                MessageReceived?.Invoke(message);
            });

            _connection.On<string, bool>("UserStatusChanged", (user, isOnline) =>
            {
                UserStatusChanged?.Invoke(user, isOnline);
            });

            _connection.On<string>("SystemMessage", (message) =>
            {
                SystemMessageReceived?.Invoke(message);
            });

            _connection.Closed += async (error) =>
            {
                ConnectionStatusChanged?.Invoke("Disconnected");
                // WithAutomaticReconnect() otomatik yeniden bağlanmayı yönetir
            };

            _connection.Reconnecting += (error) =>
            {
                ConnectionStatusChanged?.Invoke("Reconnecting...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                ConnectionStatusChanged?.Invoke("Reconnected");
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            
            // Join metodu ile gruba katıl (RegisterUser yerine)
            await _connection.InvokeAsync("Join", username);
            
            ConnectionStatusChanged?.Invoke("Connected");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Bağlantı hatası: {ex.Message}";
            if (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
            {
                errorMessage += $"\n\nHub URL: {_settings.ServerSettings.HubUrl}";
                errorMessage += "\n\n404 hatası alındı. Bu, hub endpoint'inin bulunamadığı anlamına gelir.";
                errorMessage += "\nLütfen Ayarlar menüsünden Hub URL'ini kontrol edin.";
                errorMessage += "\nYaygın endpoint formatları:";
                errorMessage += "\n- /hub";
                errorMessage += "\n- /hubs/chat";
            }
            ConnectionStatusChanged?.Invoke(errorMessage);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async Task SendMessageAsync(string from, string to, string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            // Kılavuza göre SendChatMessage metodu kullanılmalı
            await _connection.InvokeAsync("SendChatMessage", from, to, message);
        }
    }

    public async Task SendFileAsync(string to, string fileName, byte[] fileData, string email)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            var base64Data = Convert.ToBase64String(fileData);
            await _connection.InvokeAsync("SendFile", to, fileName, base64Data, email);
        }
    }

    public async Task<List<User>> GetOnlineUsersAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                // Sunucu doğrudan List<User> döndürüyor, SignalR otomatik deserialize eder
                var users = await _connection.InvokeAsync<List<User>>("GetOnlineUsers");
                return users ?? new List<User>();
            }
            catch (Exception ex)
            {
                
                throw new Exception($"Kullanıcılar yüklenirken hata: {ex.Message}");
            }
        }
        return new List<User>();
    }

    public async Task<List<ChatMessage>> GetMessageHistoryAsync(string fromUser, string toUser)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                // İki kullanıcı arasındaki mesaj geçmişini sunucudan al
                var messages = await _connection.InvokeAsync<List<ChatMessage>>("GetMessageHistory", fromUser, toUser);
                return messages ?? new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                // Metod yoksa veya hata varsa boş liste döndür
                if (ex.Message.Contains("Method does not exist"))
                {
                    return new List<ChatMessage>();
                }
                throw new Exception($"Mesaj geçmişi yüklenirken hata: {ex.Message}");
            }
        }
        return new List<ChatMessage>();
    }

    public async Task<List<ChatMessage>> GetUserMessagesAsync(string username)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                // Bir kullanıcının tüm mesajlarını sunucudan al
                var messages = await _connection.InvokeAsync<List<ChatMessage>>("GetUserMessages", username);
                return messages ?? new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                // Metod yoksa veya hata varsa boş liste döndür
                if (ex.Message.Contains("Method does not exist"))
                {
                    return new List<ChatMessage>();
                }
                throw new Exception($"Kullanıcı mesajları yüklenirken hata: {ex.Message}");
            }
        }
        return new List<ChatMessage>();
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}

