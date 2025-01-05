namespace XanFTP.Options;

public class FileSendOptions : FileOptions
{
    /// <summary>
    /// Maximum buffer size, in bytes, allocated for each thread
    /// </summary>
    public int MaxBufferSize { get; init; } = 1024 * 1024;
}