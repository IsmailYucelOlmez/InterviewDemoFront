namespace CommunicationApp.Models;

public class AppSettings
{
    public ServerSettings ServerSettings { get; set; } = new();
    public DatabaseSettings DatabaseSettings { get; set; } = new();
}

