# XanFTP: A Custom Server-Client File Transfer Protocol

XanFTP is a custom implementation of a non-standard File Transfer Protocol (FTP) designed for secure and efficient file sharing between clients and servers. Unlike traditional FTP implementations, XanFTP offers flexibility in handling file transfers with additional metadata and asynchronous capabilities.

## Features

- **Server-Client Architecture**: A robust TCP-based communication framework for managing file transfers.
- **Custom File Permissions**: Clients request permissions for file transfers, which are validated by the server using custom handlers.
- **Bidirectional File Transfers**: Supports both file sending and requesting operations, allowing dynamic data exchange.
- **Asynchronous Operations**: Fully asynchronous implementation for improved performance and scalability.
- **Extensibility**: Modular design enabling integration with custom data serializers and file handlers.
- **Error Handling**: Graceful handling of invalid requests, permission denials, and network issues.
- **Additional Metadata**: Support for transferring additional data alongside files to enrich file transfer operations.

## Technologies Used

- **C# .NET**: Core language for implementation.
- **TCP Sockets**: Underlying transport layer for communication.
- **Asynchronous Programming**: Efficient resource utilization and task management.
- **StreamExtensions**: Simplified stream operations for file and data handling.

## How It Works

1. **Server Initialization**:
    - The server listens for incoming client connections and processes file transfer requests.
    - Handles custom permission validation and ensures secure file exchanges.

2. **Client Operations**:
    - Clients connect to the server using a specified endpoint.
    - Supported operations:
        - **SendFileAsync**: Upload a file to the server.
        - **RequestFileAsync**: Request a file from the server.

3. **Data Flow**:
    - Both client and server use `XanFtpHelper` for processing file segments and managing transfer operations.
    - Communication includes metadata exchange and segmented file transfers for reliability.

## Getting Started

### Prerequisites
- .NET 6.0 or later
- Visual Studio or any compatible IDE

### Usage
1. Clone this repository.
2. Build the solution to generate the server and client executables.
3. Run the server to start accepting connections.
4. Use the client to connect to the server and perform file transfers.

### Example
```csharp
// Server
var server = new XanFtpServer(new IPEndPoint(IPAddress.Any, 12345));
server.Start();

// Client
var client = new XanFtpClient();
client.Connect(new IPEndPoint(IPAddress.Loopback, 12345));
await client.SendFileAsync(new FileSendOptions { FilePath = "path/to/file" }, null);
