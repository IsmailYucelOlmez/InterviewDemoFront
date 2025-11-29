namespace CommunicationApp.Models;

public class DatabaseSettings
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = "CommunicationDB";
    public string UserId { get; set; } = "sa";
    public string Password { get; set; } = "";
    public bool TrustServerCertificate { get; set; } = true;
}

