using System.Text.Json;

namespace FactoryServerUi;

/// <summary>
/// Remembers SHA-256 thumbprints of certificates the user explicitly trusted,
/// in %LocalAppData%\FactoryServerUi\trusted_certs.json.
/// </summary>
internal sealed class TrustedCertStore
{
    private readonly string _filePath;
    private readonly HashSet<string> _thumbprints = new(StringComparer.OrdinalIgnoreCase);

    public TrustedCertStore()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FactoryServerUi");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "trusted_certs.json");
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            List<string>? stored = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_filePath));
            if (stored is not null)
                _thumbprints.UnionWith(stored);
        }
        catch
        {
            // Corrupt store: start empty.
        }
    }

    public bool Contains(string thumbprint) => _thumbprints.Contains(thumbprint);

    public void Add(string thumbprint)
    {
        if (!_thumbprints.Add(thumbprint))
            return;

        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_thumbprints.ToList()));
        }
        catch
        {
            // Not fatal: the trust simply won't persist.
        }
    }
}
