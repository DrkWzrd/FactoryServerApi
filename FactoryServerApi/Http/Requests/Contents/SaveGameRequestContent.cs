namespace FactoryServerApi.Http.Requests.Contents;

internal class SaveGameRequestContent : FactoryServerRequestContent
{
    public SaveGameRequestContent(string saveName) : base("SaveGame")
    {
        Data = new FactoryServerRequestContentData("SaveName", saveName);
    }
}
