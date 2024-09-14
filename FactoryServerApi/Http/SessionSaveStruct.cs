namespace FactoryServerApi.Http;

public class SessionSaveStruct
{
    public string SessionName { get; }
    public IReadOnlyList<SaveHeader> SaveHeaders { get; }

    internal SessionSaveStruct(
        string sessionName,
        List<SaveHeader> saveHeaders)
    {
        SessionName = sessionName;
        SaveHeaders = saveHeaders;
    }
}
