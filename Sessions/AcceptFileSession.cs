namespace XanFTP.Sessions;

public class AcceptFileSession : FileSession, IDisposable
{
    public new ClientOperationType OperationType
    {
        get => base.OperationType;
        set => base.OperationType = value;
    }

    public override void Dispose()
    {
        NetworkStream.Dispose();
    }
}