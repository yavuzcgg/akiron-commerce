using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Akiron.Identity.Infrastructure.Security;

/// <summary>
/// The RSA key Identity signs tokens with, and the public half everyone else verifies with.
/// </summary>
/// <remarks>
/// <para>
/// Asymmetric on purpose (ADR-0011). With a shared HMAC secret every service that could
/// <em>check</em> a token could also <em>forge</em> one, so one leaked config file would
/// compromise all of them. Here only this process holds the private key; the public key
/// is published at /.well-known/jwks.json and is safe for anyone to have.
/// </para>
/// <para>
/// The key id (<c>kid</c>) is the key's own RFC 7638 thumbprint, so it changes exactly
/// when the key does — the hook key rotation in 2.4 hangs on.
/// </para>
/// </remarks>
public sealed class SigningKeyStore : IDisposable
{
    private const int KeySizeInBits = 2048;

    private readonly RSA _rsa;

    private SigningKeyStore(RSA rsa)
    {
        _rsa = rsa;

        var publicJwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsa.ExportParameters(false)));
        var keyId = Base64UrlEncoder.Encode(publicJwk.ComputeJwkThumbprint());

        publicJwk.KeyId = keyId;
        publicJwk.Use = JsonWebKeyUseNames.Sig;
        publicJwk.Alg = SecurityAlgorithms.RsaSha256;

        PublicJwk = publicJwk;
        SigningKey = new RsaSecurityKey(rsa) { KeyId = keyId };
        VerificationKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = keyId };
    }

    /// <summary>Private key. Used by the token issuer and by nothing else.</summary>
    public RsaSecurityKey SigningKey { get; }

    /// <summary>Public key only — what this service's own bearer validation uses, exactly like any other service will.</summary>
    public RsaSecurityKey VerificationKey { get; }

    /// <summary>The public key in the shape the JWKS document publishes.</summary>
    public JsonWebKey PublicJwk { get; }

    /// <summary>
    /// Loads the PEM key at <paramref name="path"/>, or creates one there when allowed.
    /// </summary>
    /// <param name="createIfMissing">
    /// True only in Development. Anywhere else a missing key is a deployment mistake, and
    /// silently minting a fresh one would invalidate every token already handed out.
    /// </param>
    public static SigningKeyStore Load(string path, bool createIfMissing)
    {
        var rsa = RSA.Create();

        try
        {
            if (File.Exists(path))
            {
                rsa.ImportFromPem(File.ReadAllText(path));
            }
            else if (createIfMissing)
            {
                rsa.KeySize = KeySizeInBits;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
            }
            else
            {
                throw new InvalidOperationException(
                    $"No signing key at '{path}'. Supply one through Identity:SigningKeyPath; only Development generates its own.");
            }

            return new SigningKeyStore(rsa);
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    public void Dispose() => _rsa.Dispose();
}
