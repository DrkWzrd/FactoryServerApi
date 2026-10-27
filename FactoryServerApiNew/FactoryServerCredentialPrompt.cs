namespace FactoryServerApiNew;

/// <summary>
/// Interactive credential prompts used by <see cref="FactoryServerSession"/> to
/// complete the login flow. Each delegate is invoked only when the corresponding
/// credential is actually needed, so a caller that already knows the admin
/// password never has to handle a prompt.
/// </summary>
public sealed class FactoryServerCredentialPrompt
{
    /// <summary>
    /// Asks the user to claim an unclaimed server. Returns the server name and
    /// the admin password to claim with. Invoked when <see cref="IFactoryServerClient.MustClaim"/>
    /// is true.
    /// </summary>
    public Func<Task<(string ServerName, ReadOnlyMemory<char> AdminPassword)>> ClaimServer { get; }

    /// <summary>
    /// Asks the user for the server's admin password to log in with. Invoked when
    /// the client is not authenticated with admin privileges and has no usable
    /// in-memory admin password.
    /// </summary>
    public Func<Task<ReadOnlyMemory<char>>> AdminPassword { get; }

    public FactoryServerCredentialPrompt(
        Func<Task<(string ServerName, ReadOnlyMemory<char> AdminPassword)>> claimServer,
        Func<Task<ReadOnlyMemory<char>>> adminPassword)
    {
        ClaimServer = claimServer ?? throw new ArgumentNullException(nameof(claimServer));
        AdminPassword = adminPassword ?? throw new ArgumentNullException(nameof(adminPassword));
    }
}
