using FactoryServerApi.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FactoryServerUi;

/// <summary>
/// Valid certificates pass. Untrusted (e.g. self-signed) ones are shown to the user;
/// a "yes" is remembered in the <see cref="TrustedCertStore"/> (pinned by full-certificate SHA-256).
/// </summary>
internal sealed class CertTrustValidator : IServerCertificateValidationStrategy
{
    /// <param name="CertFingerprintHex">SHA-256 of the full certificate (used for pinning).</param>
    /// <param name="ServerLogFingerprintHex">SHA-256 of the public key's PEM text (what the server console prints).</param>
    public readonly record struct CertCheckResult(
        string CertFingerprintHex,
        string ServerLogFingerprintHex, string ServerLogFingerprintB64,
        string Subject, bool ChainValid, bool Allowed);

    private readonly Form _owner;
    private readonly TrustedCertStore _store;
    private readonly string _target;

    /// <summary>Raised (on the HTTP thread) after every certificate check, so the UI can log it.</summary>
    public event Action<CertCheckResult>? CertificateChecked;

    public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> CustomValidationCallback { get; }

    public CertTrustValidator(Form owner, TrustedCertStore store, string target)
    {
        _owner = owner;
        _store = store;
        _target = target;
        CustomValidationCallback = Validate;
    }

    private bool Validate(HttpRequestMessage request, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        bool chainValid = errors == SslPolicyErrors.None;

        if (cert is null)
        {
            CertificateChecked?.Invoke(new("(none)", "(none)", "(none)", "(none)", chainValid, chainValid));
            return chainValid;
        }

        string certHex = Convert.ToHexString(SHA256.HashData(cert.RawData));

        // The server console prints the SHA-256 of the public key's PEM text
        // (PemEncoding has no trailing newline; the server's text has one).
        string pem = PemEncoding.WriteString("PUBLIC KEY", cert.PublicKey.ExportSubjectPublicKeyInfo()) + "\n";
        byte[] logDigest = SHA256.HashData(Encoding.ASCII.GetBytes(pem));
        string logB64 = Convert.ToBase64String(logDigest);

        bool allowed = chainValid || _store.Contains(certHex) || AskUser(cert, logB64, certHex);

        CertificateChecked?.Invoke(new(certHex, Convert.ToHexString(logDigest), logB64, cert.Subject, chainValid, allowed));
        return allowed;
    }

    /// <summary>Asks on the UI thread; remembers the certificate if the user accepts.</summary>
    private bool AskUser(X509Certificate2 cert, string logB64, string certHex)
    {
        if (_owner.IsDisposed || !_owner.IsHandleCreated)
            return false;

        string message = $$"""
        The server "{{_target}}" presents an untrusted certificate.

        Subject:
        {{FormatName(cert.SubjectName)}}

        Issuer:
        {{FormatName(cert.IssuerName)}}

        Valid: {{cert.NotBefore:yyyy-MM-dd}} → {{cert.NotAfter:yyyy-MM-dd}}

        SHA256: {{logB64}}

        Allow this certificate for future connections?
        """;

        bool yes = _owner.Invoke(() => MessageBox.Show(_owner, message,
            "Untrusted server certificate",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes);

        if (yes)
            _store.Add(certHex);
        return yes;
    }

    /// <summary>Formats a distinguished name as one indented bullet per element.</summary>
    private static string FormatName(X500DistinguishedName name, int maxLength = 40) =>
        string.Join("\n",
            name.Decode(X500DistinguishedNameFlags.UseNewLines)
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Where(e => !e.StartsWith("2.5.4.13=") && !e.StartsWith("OID.2.5.4.13=") && !e.StartsWith("DESCRIPTION=", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Length > maxLength ? e[..maxLength] + "…" : e)
                .Select(e => $"        • {e}"));

}