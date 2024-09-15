using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class DownloadSaveGameResponseData
{
    public Stream SaveFileStream { get; }

    [JsonConstructor]
    internal DownloadSaveGameResponseData(Stream saveFileStream)
    {
        SaveFileStream = saveFileStream;
    }
}
