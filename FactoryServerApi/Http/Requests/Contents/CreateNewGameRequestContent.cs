namespace FactoryServerApi.Http.Requests.Contents;

internal class CreateNewGameRequestContent : FactoryServerRequestContent
{
    public CreateNewGameRequestContent(ServerNewGameData newGameData) : base("CreateNewGame")
    {
        Data = new FactoryServerRequestContentData("NewGameData", newGameData);
    }
}
