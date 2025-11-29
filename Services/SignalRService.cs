using System;
using System.Threading.Tasks;
using CommunicationApp.Helpers;
using CommunicationApp.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace CommunicationApp.Services;

public class SignalRService : IDisposable
{
    private HubConnection? _connection;
    private readonly AppSettings _settings;

    public event Action<Message>? MessageReceived;
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
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            _connection.On<Message>("ReceiveMessage", (message) =>
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
                await Task.Delay(5000);
                if (_connection != null)
                {
                    await ConnectAsync(username);
                }
            };

            await _connection.StartAsync();
            await _connection.InvokeAsync("RegisterUser", username);
            ConnectionStatusChanged?.Invoke("Connected");
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke($"Connection Error: {ex.Message}");
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
            var messageObj = new Message
            {
                Type = "chat",
                From = from,
                To = to,
                MessageText = message,
                Timestamp = DateTime.Now
            };
            await _connection.InvokeAsync("SendMessage", messageObj);
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
            var usersJson = await _connection.InvokeAsync<string>("GetOnlineUsers");
            return JsonConvert.DeserializeObject<List<User>>(usersJson) ?? new List<User>();
        }
        return new List<User>();
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}

