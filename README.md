# Socket Communication Project

## Overview
This project demonstrates inter-process communication using network sockets in .NET. It consists of a socket server and client application that communicate over TCP/IP, implementing core socket programming concepts.

## Project Structure
```
├── SocketServer/
│   └── Program.cs    // Server implementation
└── SocketClient/
    └── Program.cs    // Client implementation
```

## Features
- TCP/IP socket communication
- Client-server architecture
- Bi-directional message exchange
- Multi-client support with threaded connections
- Connection management and cleanup

## Technologies
- .NET Core/6.0+
- System.Net.Sockets namespace
- TCP/IP networking

## Socket Implementation Comparison: .NET vs C

| Feature | .NET Implementation | C Implementation |
|---------|---------------------|------------------|
| Socket Creation | `new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)` | `socket(AF_INET, SOCK_STREAM, 0)` |
| Binding | `socket.Bind(ipEndPoint)` | `bind(sockfd, (struct sockaddr*)&address, sizeof(address))` |
| Listening | `socket.Listen(backlog)` | `listen(sockfd, backlog)` |
| Connection | `socket.Connect(endPoint)` | `connect(sockfd, (struct sockaddr*)&address, sizeof(address))` |
| Accepting | `Socket client = serverSocket.Accept()` | `clientfd = accept(sockfd, (struct sockaddr*)&client_addr, &addr_len)` |
| Sending | `socket.Send(buffer)` | `send(sockfd, buffer, length, flags)` |
| Receiving | `socket.Receive(buffer)` | `recv(sockfd, buffer, length, flags)` |
| Threading | `Task` or `Thread` | `pthread_create()` |
| Error Handling | Exceptions | Error codes and `errno` |
| Address Structure | `IPEndPoint` | `struct sockaddr_in` |
| Memory Management | Automatic | Manual |

## Key Concepts Learned
- Socket lifecycle (create, bind, listen, accept, receive/send, close)
- Difference between Unix domain sockets and internetwork sockets
- Port usage and management (server fixed ports vs client ephemeral ports)
- TCP connection identification via 4-tuple (source IP, source port, destination IP, destination port)
- Resource limitations in socket connections
- Thread management for multiple client connections

## Running the Project

### Server
```
cd SocketServer
dotnet run
```

### Client
```
cd SocketClient
dotnet run
```

# Understanding Sockets: Unix vs TCP

## What Are Sockets?

Sockets are communication endpoints that enable processes to exchange data, whether on the same machine or across a network. They provide a standardized API for network and inter-process communication in operating systems.

## How Sockets Work

Sockets follow a client-server model where:
1. A server creates a socket, binds it to an address, and listens for connections
2. A client creates a socket and connects to the server's address
3. Once connected, both sides can send and receive data bidirectionally
4. When communication is complete, either side can close the connection

## Socket Types Comparison

| Feature | Unix Domain Sockets | TCP/IP Sockets |
|---------|---------------------|----------------|
| **Communication Scope** | Same machine only | Local or across networks |
| **Addressing Mechanism** | File system paths | IP address + port number |
| **Address Format** | `/path/to/socket` | `192.168.1.100:8080` |
| **Performance** | Higher (avoids network stack) | Lower (requires protocol overhead) |
| **Address Family** | `AF_UNIX` / `AddressFamily.Unix` | `AF_INET` / `AddressFamily.InterNetwork` |
| **Connection Identification** | File descriptor | 4-tuple (source IP, source port, destination IP, destination port) |
| **Security** | File system permissions | Network-level security |
| **Use Cases** | Fast IPC between local processes | Communication between processes on different machines |
| **Port Usage** | No concept of ports | Requires port allocation |
| **Durability** | Socket file remains until explicitly removed | Terminates when process ends |

## Key Distinctions

### Unix Domain Sockets
- Use file paths as addresses
- Don't require network protocol overhead
- Provide higher performance for local communication
- Limited to processes on the same machine
- File permissions control access

### TCP/IP Sockets
- Use IP addresses and ports
- Work across network boundaries
- Client connections use ephemeral ports assigned by OS
- Support thousands of concurrent connections via unique 4-tuple identifiers
- Require more system resources and overhead

## Common Applications

- **Unix Sockets**: Database engines (PostgreSQL, MySQL), X Window System, Container communications
- **TCP Sockets**: Web servers/browsers, email clients/servers, remote login, file transfers

Both socket types follow similar programming patterns but differ in how they're addressed and the scope of their communication capabilities.



## Future Enhancements
- Implement asynchronous socket operations
- Add protocol definition for structured messages
- Improve error handling and recovery
- Implement connection pooling
- Add configuration options for IP/port
- Create performance benchmarks

## Resources
- [Microsoft Docs: Socket Class](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket)
- [TCP/IP Network Programming in C#](https://www.codeproject.com/Articles/1415/TCP-IP-Network-Programming-in-C)
- [.NET Socket Programming Guide](https://docs.microsoft.com/en-us/dotnet/framework/network-programming/sockets)

---
*This project was created as a learning exercise to understand socket communication in .NET.*
