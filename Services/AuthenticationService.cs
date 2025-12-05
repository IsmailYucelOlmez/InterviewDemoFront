using System.Net.Http;
using System.Text;
using CommunicationApp.Helpers;
using CommunicationApp.Models;
using Newtonsoft.Json;

namespace CommunicationApp.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuthenticationService
{
    private readonly HttpClient _httpClient;

    public AuthenticationService()
    {
        _httpClient = new HttpClient();
    }

    private static string ExtractErrorMessage(string responseContent, string fallback)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
            return fallback;

        try
        {
            var obj = JsonConvert.DeserializeObject<dynamic>(responseContent);
            if (obj == null) return responseContent;

            return (string?)obj.message
                ?? (string?)obj.Message
                ?? (string?)obj.error
                ?? (string?)obj.Error
                ?? responseContent;
        }
        catch
        {
            return responseContent;
        }
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "Kullanıcı adı ve şifre gereklidir!" 
                };
            }

            var loginRequest = new
            {
                username = username,
                password = password
            };

            var json = JsonConvert.SerializeObject(loginRequest, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = ConfigHelper.GetBaseUrl();
            var response = await _httpClient.PostAsync($"{baseUrl}/api/Auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new AuthResult { Success = true };
            }
            else
            {
                var errorMessage = ExtractErrorMessage(responseContent, "Giriş başarısız!");

                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = errorMessage 
                };
            }
        }
        catch (HttpRequestException ex)
        {
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = $"Bağlantı hatası: {ex.Message}" 
            };
        }
        catch (Exception ex)
        {
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = $"Hata: {ex.Message}" 
            };
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var registerRequest = new
            {
                Username = username,
                Email = email,
                Password = password
            };

            var json = JsonConvert.SerializeObject(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = ConfigHelper.GetBaseUrl();
            var response = await _httpClient.PostAsync($"{baseUrl}/api/Auth/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new AuthResult { Success = true };
            }
            else
            {
                var errorMessage = ExtractErrorMessage(responseContent, "Kayıt başarısız!");
                
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = errorMessage 
                };
            }
        }
        catch (HttpRequestException ex)
        {
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = $"Bağlantı hatası: {ex.Message}" 
            };
        }
        catch (Exception ex)
        {
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = $"Hata: {ex.Message}" 
            };
        }
    }
}

