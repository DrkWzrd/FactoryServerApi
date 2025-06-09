namespace FactoryServerApi.Udp;

public interface ICookieGenerator
{

    ValueTask<ulong> GetCookieAsync(TimeProvider timeProv);

}
