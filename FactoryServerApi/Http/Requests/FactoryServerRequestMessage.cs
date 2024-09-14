using System.Net.Http.Headers;

namespace FactoryServerApi.Http.Requests;
internal class FactoryServerRequestMessage : HttpRequestMessage
{

    public FactoryServerRequestMessage(FactoryGamePlayerId? playerId = null, string? authToken = null)
    {
        if (authToken is not null)
            Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        if (playerId.HasValue)
            Headers.Add("X-FactoryGame-PlayerId", playerId.Value.ToString());
    }
}
