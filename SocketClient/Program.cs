using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LSystem;

class Program
{

    static void Main(string[] args)
    {
        const string socketPath = "/tmp/dotnet_socket";

        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            var endPoint = new UnixDomainSocketEndPoint(socketPath);
            var port = 8888;
            IPAddress localAddress = IPAddress.Loopback;
            var serverEndPoint = new IPEndPoint(localAddress, port);

            clientSocket.Connect(serverEndPoint);
            Console.WriteLine("Connected to server...");

            Byte[] buffer = new Byte[1024];

            Console.WriteLine("Write any message and press enter to send");

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (input == "quit" || input is null)
                {
                    break;
                }

                Byte[] message = Encoding.UTF8.GetBytes(input);

                clientSocket.Send(message);


                // server response
                int bytesRead = clientSocket.Receive(buffer);

                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Recieved From server::: {response}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection Error::: {ex}");

        }
        finally
        {
            clientSocket.Close();
            Console.WriteLine("Close client successfully");

        }

    }





}
