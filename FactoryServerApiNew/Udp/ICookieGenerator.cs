namespace FactoryServerApiNew.Udp;

public interface ICookieGenerator
{

    ValueTask<ulong> GetCookieAsync(TimeProvider timeProv);

}
