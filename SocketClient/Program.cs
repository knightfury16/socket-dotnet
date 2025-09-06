using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LSystem;

class Program
{

    private static string SocketPath = "/tmp/dotnet_socket"; // this file path must match server

    static async Task Main(string[] args)
    {

        // var socket = CreateUnixSocket(); //Uncomment this line to create unix socket, need same modification on server
        var socket = CreateTcpSocket();

        Console.WriteLine($"Successfully created socket of type {socket.AddressFamily}.");

        try
        {
            EndPoint endPoint = default!;

            if (socket.AddressFamily == AddressFamily.Unix)
            {
                endPoint = GetUnixEndPoint();
            }
            else
            {
                endPoint = GetTcpEndPoint();
            }

            socket.Connect(endPoint);
            Console.WriteLine($"Successfully connected to server on endPoint {endPoint.ToString()}...");

            Byte[] buffer = new Byte[1024];

            var dataSent = await socket.SendAsync(Encoding.UTF8.GetBytes(GenerateLongMessage()));

            Console.WriteLine($"I have successfully sent {dataSent}");

            // server response
            int bytesRead = socket.Receive(buffer);

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"Recieved From server: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection Error::: {ex}");
        }
        finally
        {
            socket.Close();
            Console.WriteLine("Close client successfully");
        }

    }

    private static EndPoint GetTcpEndPoint()
    {
        // This is where i am connecting to.For client a random port is assigned by OS
        var port = 8888;
        IPAddress localAddress = IPAddress.Loopback;
        var endPoint = new IPEndPoint(localAddress, port);

        return endPoint;
    }

    private static EndPoint GetUnixEndPoint()
    {
        var endPoint = new UnixDomainSocketEndPoint(SocketPath);
        return endPoint;
    }
    private static Socket CreateTcpSocket()
    {
        try
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            return socket;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Socket Creation Failed. {ex}");
            throw;
        }
    }
    private static object CreateUnixSocket()
    {
        try
        {
            Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            return socket;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Socket Creation Failed. {ex}");
            throw;
        }
    }

    public static string GenerateLongMessage()
    {
        const int messageSize = 1024;

        var sb = new StringBuilder(messageSize);

        sb.Append("GET /user/login HTTP/1.1\r\n");
        sb.Append($"Content-Length: {messageSize}\r\n");
        sb.Append("Content-Type: text/plain\r\n");
        sb.Append("Authorization: Bearer 2312323\r\n");
        sb.Append("Authorization: Bearer 2312323\r\n\r\n");

        var postion = sb.Length;
        var remaining = messageSize - sb.Length;

        for (int i = 0; i < remaining; i++)
        {
            if (remaining % 500 == 0)
            {
                sb.Append($"Position: {postion + i}");
            }
            else
            {
                sb.Append($"Hello from postion: {i}");
            }
        }

        return sb.ToString();
    }
}
