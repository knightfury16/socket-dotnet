using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LSystem;

class Program
{

    private static string SocketPath = "/tmp/dotnet_socket"; // this file path must match server

    static void Main(string[] args)
    {

        // var socket = CreateUnixSocket(); //Uncomment this line to create unix socket, need same modification on server
        var socket = CreateTcpSocket();

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

            Console.WriteLine("Write any message(quit to exit) and press enter.");

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (input == "quit" || input is null)
                {
                    break;
                }

                Byte[] message = Encoding.UTF8.GetBytes(input);

                socket.Send(message);


                // server response
                int bytesRead = socket.Receive(buffer);

                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Recieved From server: {response}");
            }
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
}
