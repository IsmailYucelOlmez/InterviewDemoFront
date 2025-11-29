namespace CommunicationApp.Models;

public class ServerSettings
{
    public string ServerIp { get; set; } = "localhost";
    public int ServerPort { get; set; } = 5000;
    public string HubUrl { get; set; } = "http://localhost:5000/communicationHub";
}

