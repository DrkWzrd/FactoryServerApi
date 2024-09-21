namespace FactoryServerApi.Http.Responses;

public class DownloadSaveGameData : FactoryServerResponseContentData
{
    public Stream SaveFileStream { get; }

    internal DownloadSaveGameData(Stream saveFileStream)
    {
        SaveFileStream = saveFileStream;
    }
}
