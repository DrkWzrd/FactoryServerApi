namespace FactoryServerApi.Http.Requests.Contents;

internal class CreateNewGameContent : FactoryServerContent
{
    public CreateNewGameContent(ServerNewGameData newGameData) : base("CreateNewGame")
    {
        Data = new SinglePropertyFactoryServerContentData("NewGameData", newGameData);
    }
}
