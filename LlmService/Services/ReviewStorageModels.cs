namespace LlmService;

public sealed record StorageDownload(byte[] Bytes, string ContentType, string FileName);
