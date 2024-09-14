namespace FactoryServerApi.Http.Responses;

public class DownloadSaveGameResponseData
{
    public Stream SaveFileStream { get; }

    internal DownloadSaveGameResponseData(Stream saveFileStream)
    {
        SaveFileStream = saveFileStream;
    }
}
