using System.Net.Sockets;

namespace XanFTP.Sessions;

public abstract class FileSession : IDisposable
{
    public required NetworkStream NetworkStream { get; set; }
    public string? FileName { get; set; }
    public Dictionary<string, dynamic>? AdditionalData { get; set; }
    
    public ServerAcceptHandler? AcceptHandler { get; set; }

    public ClientOperationType OperationType { get; set; }
    
    public Options.FileOptions? FileOptions { get; set; }
    public abstract void Dispose();
}