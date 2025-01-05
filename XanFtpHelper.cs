using StreamExtensions;
using XanFTP.Options;
using XanFTP.Sessions;
using FileOptions = System.IO.FileOptions;

namespace XanFTP;

internal static class XanFtpHelper
{
    private static async Task StartLoadingFileSegmentsTask(SendFileSession session,
        CancellationToken cancellationToken = default)
    {
        if (session.FileOptions?.FilePath == null)
        {
            throw new InvalidOperationException("The file path is null.");
        }

        if (session.FileOptions is not FileSendOptions fileOptions)
        {
            throw new InvalidOperationException("The file options is null.");
        }

        await using var fileStream = new FileStream(session.FileOptions.FilePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, FileOptions.Asynchronous);
        while (fileStream.Position != fileStream.Length)
        {
            var buffer = fileStream.Length - fileStream.Position < fileOptions.MaxBufferSize
                ? new byte[fileStream.Length - fileStream.Position]
                : new byte[fileOptions.MaxBufferSize];

            var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) return;
            session.FileSegments.Add(buffer, cancellationToken);
        }

        session.FileSegments.CompleteAdding();
    }

    private static async Task StartSendingFileSegmentsTask(SendFileSession session,
        CancellationToken cancellationToken = default)
    {
        var networkStream = session.NetworkStream;

        while (!session.FileSegments.IsCompleted)
        {
            try
            {
                var fileSegment = session.FileSegments.Take(cancellationToken);
                await networkStream.WriteAsync(BitConverter.GetBytes((uint)SendFileOperation.SendFileSegment),
                    cancellationToken);
                await networkStream.WriteAsync(BitConverter.GetBytes((uint)fileSegment.Length), cancellationToken);
                await networkStream.WriteAsync(fileSegment, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }

        await networkStream.WriteAsync(BitConverter.GetBytes((uint)SendFileOperation.EndFileSending),
            cancellationToken);
    }

    public static async Task SendFileAsync(SendFileSession session, CancellationToken cancellationToken = default)
    {
        var loadingFileSegmentsTask = StartLoadingFileSegmentsTask(session, cancellationToken);
        var sendFileSegmentsTask = StartSendingFileSegmentsTask(session, cancellationToken);

        await loadingFileSegmentsTask;
        await sendFileSegmentsTask;
    }

    public static async Task AcceptFileAsync(AcceptFileSession acceptFileSession,
        CancellationToken cancellationToken = default)
    {
        if (acceptFileSession.FileOptions is not FileAcceptOptions options)
        {
            throw new InvalidCastException("FileOptions is not FileAcceptOptions");
        }

        if (acceptFileSession.FileOptions.FilePath == null)
        {
            throw new InvalidOperationException(
                "acceptFileSession is not in its valid state. " +
                "Ensure its properties are initialized before calling this method.");
        }

        var networkStream = acceptFileSession.NetworkStream;

        var filePath = Path.Combine(options.OutputDirectory,
            acceptFileSession.FileOptions.FilePath);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096,
            FileOptions.Asynchronous);
        try
        {
            var isFileSendingInProgress = true;
            while (isFileSendingInProgress)
            {
                var operation = (SendFileOperation)await networkStream.ReadUInt32Async(cancellationToken);
                switch (operation)
                {
                    case SendFileOperation.SendFileSegment:
                        var segmentSize = await networkStream.ReadUInt32Async(cancellationToken);
                        var segmentBytes = new byte[segmentSize];
                        await networkStream.ReadExactBytesAsync(segmentBytes, segmentBytes.Length, cancellationToken);
                        await fileStream.WriteAsync(segmentBytes, cancellationToken);
                        await fileStream.FlushAsync(cancellationToken);
                        break;
                    case SendFileOperation.EndFileSending:
                        isFileSendingInProgress = false;
                        break;
                    case SendFileOperation.RequestSendPermission:
                    default:
                        continue;
                }
            }
        }
        catch (IOException)
        {
        }
        finally
        {
            await fileStream.FlushAsync(cancellationToken);
        }
    }
}