using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzKview.Core.Services;

public sealed record KeyVaultSecretInfo(
    string Name,
    string? ContentType,
    bool? Enabled,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpiresOn,
    IReadOnlyDictionary<string, string>? Tags
);

public interface IKeyVaultService
{
    /// <summary>
    /// Lists metadata for all secrets in the configured Key Vault. Secret values are NOT returned.
    /// </summary>
    Task<IReadOnlyList<KeyVaultSecretInfo>> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest value of a secret by name.
    /// </summary>
    Task<string?> GetSecretValueAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates the value of a secret. Returns the new value's version Id.
    /// </summary>
    Task<string> SetSecretValueAsync(string name, string value, string? contentType = null, CancellationToken cancellationToken = default);
}
