
namespace CommunicationApp.Models;

public class ChatMessage
{
    public string Type { get; set; } = "chat";
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
}

