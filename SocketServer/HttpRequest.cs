namespace SocketServer;

public class HttpRequest
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int? ContentLength { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? ContentType { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool KeepAlive { get; set; }
}
