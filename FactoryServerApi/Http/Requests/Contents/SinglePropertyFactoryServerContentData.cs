namespace FactoryServerApi.Http.Requests.Contents;

internal class SinglePropertyFactoryServerContentData : IFactoryServerContentData
{

    private readonly string _propertyName;
    private readonly object _data;

    public SinglePropertyFactoryServerContentData(string propertyName, object? data)
    {
        _propertyName = propertyName;
        _data = data ?? string.Empty;
    }

    public object GetJson()
    {
        return new Dictionary<string, object>()
        {
            {_propertyName, _data },
        };
    }
}
