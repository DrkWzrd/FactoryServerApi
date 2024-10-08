﻿namespace FactoryServerApi.Http.Requests.Contents;

internal class SaveGameContent : FactoryServerContent
{
    public SaveGameContent(string saveName) : base("SaveGame")
    {
        Data = new SinglePropertyFactoryServerContentData("SaveName", saveName);
    }
}
