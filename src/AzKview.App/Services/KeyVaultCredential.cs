using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using AzKview.Core.Services;

namespace AzKview.App.Services;

/// <summary>
/// TokenCredential that bridges the app's IAuthService (MSAL) to Azure SDK clients.
/// </summary>
public sealed class KeyVaultCredential : TokenCredential
{
    private readonly IAuthService _authService;

    public KeyVaultCredential(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var scopes = requestContext.Scopes is { Length: > 0 } ? requestContext.Scopes : new[] { "https://vault.azure.net/.default" };
        string? token = await _authService.AcquireTokenAsync(scopes);
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationFailedException("Failed to acquire token for Azure Key Vault.");
        }
        // Expiration unknown from IAuthService; provide a short-lived placeholder to avoid caching too long.
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(5);
        return new AccessToken(token, expiresOn);
    }
}
