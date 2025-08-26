using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    private static int _clientId = 0;

    static void Main(string[] args)
    {
        const string socketPath = "/tmp/dotnet_socket"; // this is like an address, that client can recognize and connect to

        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            var endPoint = new UnixDomainSocketEndPoint(socketPath);
            IPAddress localAddress = IPAddress.Loopback;
            int port = 8888;
            var localEndPoint = new IPEndPoint(localAddress, port);

            socket.Bind(localEndPoint);
            Console.WriteLine($"Socket bound to {socketPath}");

            socket.Listen(5);
            Console.WriteLine("Multi client server socket listing for client...");

            while (true)
            {
                try
                {
                    var clientSocket = socket.Accept(); // server socket is creating a new socket to handle this communication, while server goes back to listing
                    var clientId = ++_clientId;

                    Console.WriteLine($"Client with ID: {clientId} connected!");

                    Thread clientThread = new Thread(() => HandleClient(clientSocket, clientId));
                    clientThread.IsBackground = true; // kill all thread when main thread is terminated
                    clientThread.Start();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client::: {ex}");
                }
            }


        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Server Error:::{ex}");
            throw;
        }
        finally
        {
            socket.Close();

            if (File.Exists(socketPath))
            {
                File.Delete(socketPath);
            }

            Console.WriteLine("Server closed and cleaned up");
        }
    }

    private static void HandleClient(Socket clientSocket, int clientId)
    {
        Console.WriteLine($"Client Handle with client ID {clientId} started");

        Byte[] buffer = new Byte[1024];

        try
        {
            while (true)
            {
                var bufferSize = clientSocket.Receive(buffer);

                if (bufferSize == 0)
                {
                    Console.WriteLine($"Client with Id {clientId} disconnected safely");
                    break;
                }
                if (bufferSize < 0)
                {
                    Console.WriteLine("Disconnecting client");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bufferSize);

                Console.WriteLine($"Server Recieved: {message}");

                //echo back the message
                Byte[] response = Encoding.UTF8.GetBytes(message);
                clientSocket.Send(response);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {clientId} Error:::{ex}");

        }
        finally
        {
            clientSocket.Close();
            Console.WriteLine($"Cleanly disposed client socket with id {clientId} thread");
        }
    }
}
