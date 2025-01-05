using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using StreamExtensions;
using XanFTP.Options;
using XanFTP.Sessions;

namespace XanFTP;

public class XanFtpClient : IDisposable
{
    private IPEndPoint? _listenerEndPoint;

    public void Connect(IPEndPoint endPoint)
    {
        _listenerEndPoint = endPoint;
    }

    public async Task SendFileAsync(FileSendOptions options,
        Dictionary<string, dynamic>? additionalData,
        CancellationToken cancellationToken = default)
    {
        // Checks if server's endpoint was initialized
        if (_listenerEndPoint == null)
        {
            throw new InvalidOperationException("Call Connect() first.");
        }

        // Initializes TcpClient and connects to server.
        using var connectionClient = new TcpClient();
        await connectionClient.ConnectAsync(_listenerEndPoint, cancellationToken);

        // Initializes SendFileSession class instance, which contains all data for file sending.
        var networkStream = connectionClient.GetStream();
        using var fileSendSession = new SendFileSession
        {
            NetworkStream = networkStream
        };
        fileSendSession.AdditionalData = additionalData;
        fileSendSession.OperationType = ClientOperationType.SendFile;
        fileSendSession.FileOptions = options;

        // Gets and checks the permission for file send, which is granted or rejected by server.
        // It also sends additional data
        var isPermissionGranted = await GetPermissionAsync(fileSendSession, cancellationToken);

        if (!isPermissionGranted)
        {
            throw new UnauthorizedAccessException("Acceptor refused to accept file");
        }
        
        // After permission is granted, sends file.
        await XanFtpHelper.SendFileAsync(fileSendSession, cancellationToken);
    }

    public async Task RequestFileAsync(RequestFileHandler requestFileHandler,
        Dictionary<string, dynamic>? additionalData, CancellationToken cancellationToken = default)
    {
        // Checks if server's endpoint was initialized
        if (_listenerEndPoint == null)
        {
            throw new InvalidOperationException("Call Connect() first.");
        }

        // Initializes TcpClient and connects to server.
        using var connectionClient = new TcpClient();
        await connectionClient.ConnectAsync(_listenerEndPoint, cancellationToken);

        // Initializes SendFileSession class instance, which contains all data for file sending.
        var networkStream = connectionClient.GetStream();
        using var fileAcceptSession = new AcceptFileSession
        {
            NetworkStream = networkStream,
            AdditionalData = additionalData
        };
        
        fileAcceptSession.OperationType = ClientOperationType.RequestFile;
        fileAcceptSession.FileOptions = requestFileHandler();

        // Gets and checks the permission for file accept, which is granted or rejected by server.
        var isPermissionGranted =
            await GetPermissionAsync(fileAcceptSession, cancellationToken);

        if (!isPermissionGranted)
        {
            throw new UnauthorizedAccessException("Sender refused to send file");
        }

        await XanFtpHelper.AcceptFileAsync(fileAcceptSession, cancellationToken);
    }

    private async Task<bool> GetPermissionAsync(FileSession fileSession, CancellationToken cancellationToken = default)
    {
        if (fileSession.OperationType == ClientOperationType.Invalid)
        {
            throw new InvalidOperationException("Invalid operation type");
        }

        if (fileSession.FileOptions == null)
        {
            throw new InvalidOperationException("FileOptions is null");
        }

        var networkStream = fileSession.NetworkStream;

        // Sends Operation code.
        await networkStream.WriteAsync(
            BitConverter.GetBytes((uint)(
                fileSession.OperationType == ClientOperationType.RequestFile
                    ? SendFileOperation.RequestFileRequestPermission
                    : SendFileOperation.RequestSendPermission
            )),
            cancellationToken);

        // Sends Bool, representing whether there is an additional data or no.
        await networkStream.WriteAsync(BitConverter.GetBytes(fileSession.AdditionalData != null),
            cancellationToken);

        if (fileSession.AdditionalData != null)
        {
            // Sends Additional data.
            await networkStream.WriteStringAsync<uint>(JsonSerializer.Serialize(fileSession.AdditionalData),
                cancellationToken);
        }

        // Sends File Path(Name).
        await networkStream.WriteStringAsync<uint>(Path.GetFileName(fileSession.FileOptions.FilePath), cancellationToken);

        // Reads whether the permission is granted or no.
        var isPermissionGranted = await networkStream.ReadBooleanAsync(cancellationToken);

        return isPermissionGranted;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}