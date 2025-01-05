using System.Collections.Concurrent;
using System.Net.Sockets;
using XanFTP.Options;

namespace XanFTP.Sessions;

public class SendFileSession : FileSession, IDisposable
{
    public BlockingCollection<byte[]> FileSegments { get; } = [];

    public new ClientOperationType OperationType
    {
        get => base.OperationType;
        set => base.OperationType = value;
    }

    public override void Dispose()
    {
        FileSegments.Dispose();
    }
}