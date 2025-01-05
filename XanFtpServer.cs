using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using StreamExtensions;
using XanFTP.Sessions;

namespace XanFTP;

public enum ClientOperationType
{
    Invalid,
    SendFile,
    RequestFile
}

public class XanFtpServer(IPEndPoint localEndPoint) : IDisposable
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new DynamicJsonConverter() }
    };

    public void Start()
    {
        Server.Start();
    }

    public void Start(int backlog)
    {
        Server.Start(backlog);
    }

    public void Stop()
    {
        Server.Stop();
    }

    public async Task AcceptConnectionAsync(ServerAcceptHandler acceptHandler, CancellationToken cancellationToken = default)
    {
        using var fileTransferConnection = await Server.AcceptTcpClientAsync(cancellationToken);
        var networkStream = fileTransferConnection.GetStream();

        var operationType = await ReadOperationType(networkStream, cancellationToken);

        if (operationType == ClientOperationType.Invalid) return;

        FileSession fileSession;
        if (operationType == ClientOperationType.RequestFile)
        {
            fileSession = new SendFileSession
            {
                NetworkStream = networkStream,
                AcceptHandler = acceptHandler,
                OperationType = operationType
            };
            
            await BuildFileSession(fileSession, cancellationToken);
            await ProcessPermission(fileSession, cancellationToken);

            await XanFtpHelper.SendFileAsync((SendFileSession)fileSession, cancellationToken);
        }
        else
        {
            fileSession = new AcceptFileSession
            {
                NetworkStream = networkStream,
                AcceptHandler = acceptHandler,
                OperationType = operationType
            };
            
            await BuildFileSession(fileSession, cancellationToken);
            await ProcessPermission(fileSession, cancellationToken);
            
            await XanFtpHelper.AcceptFileAsync((AcceptFileSession)fileSession, cancellationToken);
        }
    }

    private static async Task ProcessPermission(FileSession fileSession, CancellationToken cancellationToken)
    {
        if (fileSession.AcceptHandler == null)
        {
            await fileSession.NetworkStream.WriteAsync(BitConverter.GetBytes(true) ,cancellationToken);
            return;
        }

        if (fileSession.FileName == null)
        {
            throw new InvalidOperationException("File path is null");
        }
        
        fileSession.FileOptions = fileSession.AcceptHandler.Invoke(fileSession.OperationType, fileSession.FileName, fileSession.AdditionalData);

        await fileSession.NetworkStream.WriteAsync(
            BitConverter.GetBytes(fileSession.FileOptions != null),
            cancellationToken);
    }

    private static async Task BuildFileSession(FileSession fileSession, CancellationToken cancellationToken = default)
    {
        var networkStream = fileSession.NetworkStream;
        if (await networkStream.ReadBooleanAsync(cancellationToken))
        {
            var additionalData = await networkStream.ReadStringAsync<uint>(cancellationToken);
            try
            {
                fileSession.AdditionalData =
                    JsonSerializer.Deserialize<Dictionary<string, dynamic>>(additionalData, JsonSerializerOptions);
            }
            catch (JsonException)
            {
                
            }
        }

        fileSession.FileName = await networkStream.ReadStringAsync<uint>(cancellationToken);
    }

    private static async Task<ClientOperationType> ReadOperationType(NetworkStream networkStream,
        CancellationToken cancellationToken = default)
    {
        var operation = (SendFileOperation)await networkStream.ReadUInt32Async(cancellationToken);

        return operation switch
        {
            SendFileOperation.RequestSendPermission => ClientOperationType.SendFile,
            SendFileOperation.RequestFileRequestPermission => ClientOperationType.RequestFile,
            _ => ClientOperationType.Invalid
        };
    }

    public TcpListener Server { get; } = new(localEndPoint);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Server.Stop();
        Server.Dispose();
    }
}