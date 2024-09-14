namespace FactoryServerApi.Http.Requests.Contents;

internal class DictionaryFactoryServerContentData : IFactoryServerContentData
{

    private readonly IReadOnlyDictionary<string, object?> _data;

    public DictionaryFactoryServerContentData(IReadOnlyDictionary<string, object?> data)
    {
        _data = data;
    }

    public object GetJson()
    {
        return _data;
    }
}
