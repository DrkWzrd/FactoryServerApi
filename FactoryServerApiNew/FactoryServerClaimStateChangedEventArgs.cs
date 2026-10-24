namespace FactoryServerApiNew;

/// <summary>
/// Event args for the <see cref="IFactoryServerClient.ClaimStateChanged"/> event.
/// </summary>
public sealed class FactoryServerClaimStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The new claim state of the server. Claiming is irreversible by the client side.
    /// </summary>
    public bool IsClaimed { get; }

    public FactoryServerClaimStateChangedEventArgs(bool isClaimed)
    {
        IsClaimed = isClaimed;
    }
}
