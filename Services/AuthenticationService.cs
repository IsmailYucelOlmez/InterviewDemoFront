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
                string errorMessage = "Giriş başarısız!";
                try
                {
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        try
                        {
                            var errorObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                            if (errorObj != null)
                            {
                                // Try different possible error message fields
                                if (errorObj.message != null)
                                {
                                    errorMessage = errorObj.message.ToString() ?? errorMessage;
                                }
                                else if (errorObj.Message != null)
                                {
                                    errorMessage = errorObj.Message.ToString() ?? errorMessage;
                                }
                                else if (errorObj.error != null)
                                {
                                    errorMessage = errorObj.error.ToString() ?? errorMessage;
                                }
                                else if (errorObj.Error != null)
                                {
                                    errorMessage = errorObj.Error.ToString() ?? errorMessage;
                                }
                            }
                        }
                        catch { }
                        
                        if (errorMessage == "Giriş başarısız!")
                        {
                            errorMessage = responseContent;
                        }
                    }
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        errorMessage = responseContent;
                    }
                }

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
                string errorMessage = "Kayıt başarısız!";
                try
                {
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        if (errorObj != null)
                        {
                            try
                            {
                                if (errorObj.message != null)
                                {
                                    errorMessage = errorObj.message.ToString() ?? errorMessage;
                                }
                            }
                            catch { }
                            
                            if (errorMessage == "Kayıt başarısız!")
                            {
                                try
                                {
                                    if (errorObj.Message != null)
                                    {
                                        errorMessage = errorObj.Message.ToString() ?? errorMessage;
                                    }
                                }
                                catch { }
                            }
                            
                            if (errorMessage == "Kayıt başarısız!")
                            {
                                errorMessage = responseContent;
                            }
                        }
                        else
                        {
                            errorMessage = responseContent;
                        }
                    }
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        errorMessage = responseContent;
                    }
                }

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

