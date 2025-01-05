namespace XanFTP;

public delegate Options.FileOptions? ServerAcceptHandler(ClientOperationType operationType, string filePath, Dictionary<string, dynamic>? additionalData);
public delegate Options.FileOptions? RequestFileHandler();