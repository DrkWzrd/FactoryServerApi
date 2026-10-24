using System.Text.Json;

namespace FactoryServerApiNew;

/// <summary>
/// Persists per-server claim state in %LocalAppData%\FactoryServerApi\{host}_{port}.json.
/// Claiming a server is irreversible by the client side, so the state must survive
/// across client restarts: the InitialAdmin token (valid until the server is claimed),
/// the fact that the server has been claimed, and the admin password for re-login.
/// </summary>
internal sealed class FactoryServerClaimStore
{
    private readonly string _filePath;

    public bool IsClaimed { get; set; }
    public string? InitialAdminToken { get; set; }
    public string? AdminPassword { get; set; }

    public FactoryServerClaimStore(string host, int port)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safeHost = new(string.Concat(host.Select(c => invalidChars.Contains(c) ? '_' : c)));
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(FactoryServerApiNew));
        _filePath = Path.Combine(directory, $"{safeHost}_{port}.json");
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            ClaimStoreData? data = JsonSerializer.Deserialize<ClaimStoreData>(File.ReadAllText(_filePath));
            IsClaimed = data?.IsClaimed ?? false;
            InitialAdminToken = data?.InitialAdminToken;
            AdminPassword = data?.AdminPassword;
        }
        catch
        {
            // Corrupt or unreadable file: start with a clean state.
        }
    }

    public void Save()
    {
        string directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);
        ClaimStoreData data = new()
        {
            IsClaimed = IsClaimed,
            InitialAdminToken = InitialAdminToken,
            AdminPassword = AdminPassword
        };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(data));
    }

    private sealed class ClaimStoreData
    {
        public bool IsClaimed { get; set; }
        public string? InitialAdminToken { get; set; }
        public string? AdminPassword { get; set; }
    }
}
