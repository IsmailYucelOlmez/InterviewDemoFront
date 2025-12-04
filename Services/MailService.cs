using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommunicationApp.Helpers;
using Newtonsoft.Json;

namespace CommunicationApp.Services;

public class MailService
{
    private readonly HttpClient _httpClient;

    public MailService()
    {
        _httpClient = new HttpClient();
    }

    public async Task SendFileByMailAsync(string fromUser, string toUser, string mailToAddress, string fileName, byte[] fileBytes)
    {
        try
        {
            var baseUrl = ConfigHelper.GetBaseUrl();
            var url = $"{baseUrl}/api/Mail/sendFile";

            var payload = new
            {
                FromUser = fromUser,
                ToUser = toUser,
                MailToAddress = mailToAddress,
                FileName = fileName,
                FileBytes = fileBytes
            };

            var json = JsonConvert.SerializeObject(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Dosya mail ile gönderilemedi: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Mail sunucusuna bağlanırken hata oluştu: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Mail ile dosya gönderilirken hata oluştu: {ex.Message}", ex);
        }
    }
}


