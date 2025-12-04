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

    public ChatService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<ChatMessage>> GetMessageHistoryAsync(string fromUser, string toUser)
    {
        try
        {
            var baseUrl = ConfigHelper.GetBaseUrl();
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

    public async Task<List<ChatMessage>> GetUserMessagesAsync(string username)
    {
        try
        {
            var baseUrl = ConfigHelper.GetBaseUrl();
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

