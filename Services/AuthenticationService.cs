using System.Net.Http;
using System.Text;
using CommunicationApp.Helpers;
using CommunicationApp.Models;
using Newtonsoft.Json;

namespace CommunicationApp.Services;

public class AuthenticationService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;

    public AuthenticationService()
    {
        _settings = ConfigHelper.GetSettings();
        _httpClient = new HttpClient();
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var md5Hash = HashHelper.ComputeMD5Hash(password);
            var sha1Hash = HashHelper.ComputeSHA1Hash(password);

            var loginRequest = new
            {
                Username = username,
                PasswordHashMD5 = md5Hash,
                PasswordHashSHA1 = sha1Hash
            };

            var json = JsonConvert.SerializeObject(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = $"http://{_settings.ServerSettings.ServerIp}:{_settings.ServerSettings.ServerPort}";
            var response = await _httpClient.PostAsync($"{baseUrl}/api/auth/login", content);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

