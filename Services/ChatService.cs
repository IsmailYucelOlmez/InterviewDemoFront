using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunicationApp.Helpers;
using CommunicationApp.Models;
using Newtonsoft.Json;

namespace CommunicationApp.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public ChatService()
    {
        _httpClient = new HttpClient();
        _settings = ConfigHelper.GetSettings();
    }

    private string GetBaseUrl()
    {
        var hubUrl = _settings.ServerSettings.HubUrl;
        // HubUrl'den base URL'i çıkar (örn: http://localhost:5267/hub -> http://localhost:5267)
        if (Uri.TryCreate(hubUrl, UriKind.Absolute, out var uri))
        {
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }
        // Fallback: ServerIp ve ServerPort kullan
        var scheme = hubUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
        return $"{scheme}://{_settings.ServerSettings.ServerIp}:{_settings.ServerSettings.ServerPort}";
    }

    /// <summary>
    /// İki kullanıcı arasındaki mesaj geçmişini getirir
    /// GET /api/chat/history?from=user1&to=user2
    /// </summary>
    public async Task<List<ChatMessage>> GetMessageHistoryAsync(string fromUser, string toUser)
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var url = $"{baseUrl}/api/chat/history?from={Uri.EscapeDataString(fromUser)}&to={Uri.EscapeDataString(toUser)}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(json);
                return messages ?? new List<ChatMessage>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Mesaj geçmişi alınamadı: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Bağlantı hatası: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Mesaj geçmişi yüklenirken hata: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Bir kullanıcının tüm mesajlarını getirir
    /// GET /api/chat/user/{username}/messages
    /// </summary>
    public async Task<List<ChatMessage>> GetUserMessagesAsync(string username)
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var url = $"{baseUrl}/api/chat/user/{Uri.EscapeDataString(username)}/messages";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(json);
                return messages ?? new List<ChatMessage>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Kullanıcı mesajları alınamadı: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Bağlantı hatası: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Kullanıcı mesajları yüklenirken hata: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Bir kullanıcının belirli bir kullanıcıyla olan mesajlarını filtreler
    /// </summary>
    public async Task<List<ChatMessage>> GetMessagesWithUserAsync(string currentUser, string otherUser)
    {
        // Önce kullanıcının tüm mesajlarını al
        var allMessages = await GetUserMessagesAsync(currentUser);
        
        // Seçili kullanıcıyla olan mesajları filtrele
        var filteredMessages = allMessages.Where(m => 
            (m.From == currentUser && m.To == otherUser) ||
            (m.From == otherUser && m.To == currentUser)
        ).ToList();
        
        return filteredMessages;
    }
}

