using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    private static int _clientId = 0;

    // For unix socket the end point is a file descriptor
    private static string SocketPath = "/tmp/dotnet_socket"; // this is like a meet up point for client to come and connect

    static void Main(string[] args)
    {
        // var socket = CreateUnixSocket(); // uncomment this to create unix socket, need same modificatin in client
        var socket = CreateTcpSocket();

        Console.WriteLine($"Successfully created socket of type {socket.AddressFamily}.");

        try
        {
            EndPoint endPoint = default!; // socket is binded  to endpoint

            if (socket.AddressFamily == AddressFamily.Unix)
            {
                endPoint = GetUnixSocketEndPoint();
            }
            else
            {
                endPoint = GetTcpSocketEndPoint();
            }

            socket.Bind(endPoint);
            Console.WriteLine($"Successfully binded socket to endpoint {endPoint.ToString()}");

            // Accepting connection take time, backlog value is setting the max queue size
            // while socket is accepting connection from client.
            // Exceeding this throws error
            socket.Listen(5); // 5 here is the backlog value
            Console.WriteLine("Multi client server socket listening for client...");

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

            if (File.Exists(SocketPath))
            {
                File.Delete(SocketPath);
            }

            Console.WriteLine("Server closed and cleaned up");
        }
    }

    private static void HandleClient(Socket clientSocket, int clientId)
    {
        Console.WriteLine($"Client Handle with client ID {clientId} started");

        Byte[] buffer = new Byte[1024];
        Pipe pipe = new();

        // Need to fill the pipe writer from where the reader can read
        Task filling = FillPipeAsync(clientSocket, pipe.Writer); // this is running in parallel, not awaiting

        try
        {
            // This is where i read the data


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

    private static async Task FillPipeAsync(Socket clientSocket, PipeWriter writer)
    {
        int minimumBufferSize = 1024;

        try
        {
            while (true)
            {
                Memory<byte> writerBuffer = writer.GetMemory(minimumBufferSize);
                int bytesRecived = await clientSocket.ReceiveAsync(writerBuffer, SocketFlags.None);

                if (bytesRecived == 0)
                {
                    Console.WriteLine("TCP Stream finish, closing the socket connection");
                    break;
                }

                writer.Advance(bytesRecived);

                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    //reader called off the is complete
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error filling pipe. Exception: {ex}");
        }
        finally
        {
            await writer.CompleteAsync();
        }

    }


    private static Socket CreateUnixSocket()
    {
        try
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            return socket;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Socket Creation Failed. {ex}");
            throw;
        }
    }

    private static Socket CreateTcpSocket()
    {
        try
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            return socket;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Socket Creation Failed. {ex}");
            throw;
        }
    }

    private static EndPoint GetTcpSocketEndPoint()
    {
        // For Tcp we need a pair of address, the IP and Port
        // Via this address client can connect to the server
        IPAddress localAddress = IPAddress.Loopback; // local ip
        int port = 8888;

        var ipEndPoint = new IPEndPoint(localAddress, port);

        return ipEndPoint;
    }


    private static EndPoint GetUnixSocketEndPoint()
    {

        // if previously exist delete the file
        if (File.Exists(SocketPath))
        {
            File.Delete(SocketPath);
        }

        var endPoint = new UnixDomainSocketEndPoint(SocketPath);

        return endPoint;
    }


}
