using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using AzKview.Core.Services;

namespace AzKview.App.Services;

/// <summary>
/// RBAC-based Key Vault service backed by Azure SDK SecretsClient.
/// Uses an MSAL-powered TokenCredential via IAuthService's AcquireTokenAsync for interactive user.
/// </summary>
public sealed class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _client;

    public KeyVaultService(Uri vaultUri, TokenCredential credential)
    {
        _client = new SecretClient(vaultUri, credential);
    }

    public async Task<IReadOnlyList<KeyVaultSecretInfo>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<KeyVaultSecretInfo>();
        await foreach (var props in _client.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            list.Add(new KeyVaultSecretInfo(
                props.Name,
                props.ContentType,
                props.Enabled,
                props.UpdatedOn,
                props.NotBefore,
                props.ExpiresOn,
                props.Tags is null ? null : new System.Collections.Generic.Dictionary<string, string>(props.Tags)
            ));
        }
        return list;
    }

    public async Task<string?> GetSecretValueAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Secret name is required", nameof(name));
        try
        {
            KeyVaultSecret secret = await _client.GetSecretAsync(name, cancellationToken: cancellationToken);
            return secret.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<string> SetSecretValueAsync(string name, string value, string? contentType = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Secret name is required", nameof(name));
        if (value is null) throw new ArgumentNullException(nameof(value));

        var kvSecret = new KeyVaultSecret(name, value)
        {
            Properties = { ContentType = contentType }
        };

        KeyVaultSecret result = await _client.SetSecretAsync(kvSecret, cancellationToken);
        return result.Properties.Version ?? string.Empty;
    }
}
