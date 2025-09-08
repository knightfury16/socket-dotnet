using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SocketServer;

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

    private static async Task HandleClient(Socket clientSocket, int clientId)
    {
        Console.WriteLine($"Client Handle with client ID {clientId} started");

        Pipe pipe = new();

        // Need to fill the pipe writer from where the reader can read
        Task filling = FillPipeAsync(clientSocket, pipe.Writer); // this is running in parallel, not awaiting

        try
        {
            // This is where i read the data
            //
            PipeReader pipeReader = pipe.Reader;

            while (true)
            {
                Console.WriteLine("Pipe Reader waiting for data");
                ReadResult result = await pipeReader.ReadAsync();
                Console.WriteLine("Pipe Reader got data");

                ReadOnlySequence<byte> buffer = result.Buffer;


                if (TryParseHttpRequest(buffer, out HttpRequest httpRequest))
                {
                    Console.WriteLine("Parsing HttpRequest Success!!!");
                    ProcessRequest(httpRequest);

                    int _ = await clientSocket.SendAsync(Encoding.UTF8.GetBytes("Received and processed request successfully"));

                    // close of the write is request parsing is successfull
                    await pipeReader.CompleteAsync();
                    break;
                }
                else
                {
                    Console.WriteLine("Not complete data reading again");
                    pipeReader.AdvanceTo(buffer.Start, buffer.End);
                }

                if (result.IsCompleted)
                {
                    break;
                }
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

    private static void ProcessRequest(HttpRequest httpRequest)
    {
        Console.WriteLine("The Request I Received is");

        Console.WriteLine($"HttpMethod : {httpRequest.Method}");
        Console.WriteLine($"HttpPath : {httpRequest.Path}");
        Console.WriteLine($"HttpVersion : {httpRequest.Version}");


        Console.WriteLine("All Header values are: ");

        foreach (var item in httpRequest.Headers)
        {
            Console.WriteLine($"Header Key: {item.Key}, Header value: {item.Value}");
        }
    }

    private static bool TryParseHttpRequest(ReadOnlySequence<byte> buffer, out HttpRequest httpRequest)
    {
        const byte ByteCR = (byte)'\r';
        const byte ByteLF = (byte)'\n';
        ReadOnlySpan<byte> RequestLineDelimiters = [ByteLF, 0];

        httpRequest = new HttpRequest();

        SequenceReader<byte> reader = new(buffer);

        //parse requestline
        if (!reader.TryReadTo(out ReadOnlySequence<byte> requestLine, [ByteCR, ByteLF], advancePastDelimiter: false))
        {
            // not enought data till now
            return false;
        }

        var foundDelimeter = reader.TryRead(out byte next);

        // Assertion
        Debug.Assert(foundDelimeter);
        Debug.Assert(requestLine.Length > 0);

        ParseRequestLine(requestLine, ref httpRequest);

        //parse headers
        if (!reader.TryReadTo(out ReadOnlySequence<byte> headers, [ByteCR, ByteLF, ByteCR, ByteLF], advancePastDelimiter: true))
        {
            return false;
        }

        Debug.Assert(headers.Length > 0);

        ParseHeaders(headers, ref httpRequest);

        return true;
    }

    private static void ParseHeaders(ReadOnlySequence<byte> headers, ref HttpRequest httpRequest)
    {
        const byte ByteCR = (byte)'\r';
        const byte ByteLF = (byte)'\n';

        var reader = new SequenceReader<byte>(headers);

        while (true)
        {
            if (!reader.TryReadTo(out ReadOnlySpan<byte> fieldLine, [ByteCR, ByteLF], true))
            {
                break;
            }

            if (fieldLine.Length == 0)
            {
                Console.WriteLine("Field line length is zero");
            }

            var indexOfColon = fieldLine.IndexOf((byte)':');

            if (indexOfColon < 0) break;

            var headerName = fieldLine[..indexOfColon];
            var headerValue = fieldLine[(indexOfColon + 1)..];

            httpRequest.Headers.Add(Encoding.UTF8.GetString(headerName), Encoding.UTF8.GetString(headerValue).Trim());
        }
    }
    private static void ParseRequestLine(ReadOnlySequence<byte> requestLine, ref HttpRequest httpRequest)
    {

        var requestLineString = Encoding.UTF8.GetString(requestLine.FirstSpan);

        var requstLineSplit = requestLineString.Split(" ");

        Debug.Assert(requstLineSplit.Length == 3);

        string method = requstLineSplit[0];
        var path = requstLineSplit[1];
        var version = requstLineSplit[2];

        httpRequest.Method = method;
        httpRequest.Path = path;
        httpRequest.Version = version;
    }

    private static async Task FillPipeAsync(Socket clientSocket, PipeWriter writer)
    {
        Console.WriteLine("Started filling pipe");
        int minimumBufferSize = 1024;

        try
        {
            while (true)
            {
                Memory<byte> writerBuffer = writer.GetMemory(minimumBufferSize);
                int bytesRecived = await clientSocket.ReceiveAsync(writerBuffer, SocketFlags.None);

                Console.WriteLine("Received Raw data from socket");

                byte[] receivedData = writerBuffer.Span[..bytesRecived].ToArray();

                Console.WriteLine(Encoding.UTF8.GetString(receivedData));

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
                    Console.WriteLine("[Writer] closing the writer os IsCompleted true");
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
