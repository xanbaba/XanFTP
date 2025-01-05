namespace XanFTP;

public enum SendFileOperation : uint
{
    RequestSendPermission,
    SendFileSegment,
    EndFileSending,
    
    RequestFileRequestPermission
}