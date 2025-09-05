namespace SocketServer;

public class HttpResponse
{
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];

    public override string ToString()
    {
        return "Mock http response";
    }
}
