using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using CommunicationApp.Helpers;
using CommunicationApp.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CommunicationApp.Services;

public class SignalRService : IDisposable
{
    private HubConnection? _connection;
    private readonly ServerSettings _settings;

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
            var hubUrl = _settings.HubUrl;
            
            
            if (string.IsNullOrWhiteSpace(hubUrl))
            {
                throw new Exception("Hub URL ayarlanmamış. Lütfen Ayarlar menüsünden Hub URL'ini kontrol edin.");
            }
            
            
            var urlWithUsername = $"{hubUrl}?username={Uri.EscapeDataString(username)}";
            
            _connection = new HubConnectionBuilder()
                .WithUrl(urlWithUsername)
                .WithAutomaticReconnect() 
                .Build();

            
            _connection.On<JsonElement>("ReceiveMessage", (payload) =>
            {
                try
                {
                    var chatMessage = new ChatMessage
                    {
                        Type = payload.TryGetProperty("type", out var typeProp)
                            ? typeProp.GetString() ?? "chat"
                            : "chat",
                        From = payload.TryGetProperty("from", out var fromProp)
                            ? fromProp.GetString() ?? string.Empty
                            : string.Empty,
                        To = payload.TryGetProperty("to", out var toProp)
                            ? toProp.GetString() ?? string.Empty
                            : string.Empty,
                        Message = payload.TryGetProperty("message", out var messageProp)
                            ? messageProp.GetString() ?? string.Empty
                            : string.Empty,
                        Timestamp = payload.TryGetProperty("timestamp", out var tsProp)
                            && DateTime.TryParse(tsProp.GetString(), out var ts)
                                ? ts
                                : DateTime.UtcNow
                    };

                    MessageReceived?.Invoke(chatMessage);
                }
                catch
                {
                    
                }
            });

            _connection.On<string, bool>("UserStatusChanged", (user, isOnline) =>
            {
                UserStatusChanged?.Invoke(user, isOnline);
            });

            _connection.On<string>("SystemMessage", (message) =>
            {
                SystemMessageReceived?.Invoke(message);
            });

            _connection.Closed += (error) =>
            {
                ConnectionStatusChanged?.Invoke("Disconnected");
                return Task.CompletedTask;
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
            
            
            await _connection.InvokeAsync("Join", username);
            
            ConnectionStatusChanged?.Invoke("Connected");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Bağlantı hatası: {ex.Message}";
            if (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
            {
                errorMessage += $"\n\nHub URL: {_settings.HubUrl}";
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
                
                var messages = await _connection.InvokeAsync<List<ChatMessage>>("GetMessageHistory", fromUser, toUser);
                return messages ?? new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                
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
                
                var messages = await _connection.InvokeAsync<List<ChatMessage>>("GetUserMessages", username);
                return messages ?? new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                
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

