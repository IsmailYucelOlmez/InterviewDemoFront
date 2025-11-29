namespace CommunicationApp.Models;

public class Message
{
    public string Type { get; set; } = "chat";
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

