using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AzKview.Core.Services;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client.Broker;

namespace AzKview.App.Services;

public class AuthService : IAuthService
{
    private readonly string _clientId;
    private readonly string _tenantId;
    private readonly string[] _defaultScopes;
    private readonly IPublicClientApplication _pca;
    private IAccount? _account;

    public AuthService(string clientId, string tenantId, string[] defaultScopes)
    {
        _clientId = clientId;
        _tenantId = tenantId;
        _defaultScopes = defaultScopes;

        var builder = PublicClientApplicationBuilder
            .Create(_clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
            .WithRedirectUri(GetRedirectUri(_clientId));

        // Windows: enable WAM broker for native account picker & SSO
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
        }

        _pca = builder.Build();

        // Cross-platform secure token cache
        var storageProps = new StorageCreationPropertiesBuilder(
                cacheFileName: "msal_cache.dat",
                cacheDirectory: MsalCacheHelper.UserRootDirectory)
            .WithLinuxKeyring(
                schemaName: "com.azkview.tokencache",
                collection: "default",
                secretLabel: "AzKview MSAL token cache",
                attribute1: new KeyValuePair<string, string>("Version", "1"),
                attribute2: new KeyValuePair<string, string>("ProductName", "AzKview"))
            .WithMacKeyChain(
                serviceName: "com.azkview.tokencache",
                accountName: "AzKview")
            .Build();

        var cacheHelper = MsalCacheHelper.CreateAsync(storageProps).GetAwaiter().GetResult();
        cacheHelper.RegisterCache(_pca.UserTokenCache);
    }

    public string? AccountUpn => _account?.Username;
    public bool IsAuthenticated => _account != null;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public async Task<bool> SignInAsync()
    {
        try
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            _account = accounts.FirstOrDefault();

            try
            {
                // Try silent first
                var silent = await _pca.AcquireTokenSilent(_defaultScopes, _account)
                    .ExecuteAsync().ConfigureAwait(false);
                _account = silent.Account;
            }
            catch (MsalUiRequiredException)
            {
                var interactiveBuilder = _pca.AcquireTokenInteractive(_defaultScopes)
                    .WithPrompt(Prompt.SelectAccount);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // For macOS, MSAL uses system browser by default. Provide parent window handle if needed.
                    interactiveBuilder = interactiveBuilder.WithSystemWebViewOptions(new SystemWebViewOptions
                    {
                        // Leave defaults; on macOS it uses ASWebAuthenticationSession under the hood when available
                    });
                }

                var interactive = await interactiveBuilder.ExecuteAsync().ConfigureAwait(false);
                _account = interactive.Account;
            }

            AuthenticationStateChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception)
        {
            AuthenticationStateChanged?.Invoke(this, false);
            return false;
        }
    }

    public async Task SignOutAsync()
    {
        var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
        foreach (var acc in accounts)
        {
            await _pca.RemoveAsync(acc).ConfigureAwait(false);
        }
        _account = null;
        AuthenticationStateChanged?.Invoke(this, false);
    }

    public async Task<string?> AcquireTokenAsync(string[] scopes)
    {
        try
        {
            if (_account is null)
            {
                var ok = await SignInAsync().ConfigureAwait(false);
                if (!ok) return null;
            }

            try
            {
                var silent = await _pca.AcquireTokenSilent(scopes, _account)
                    .ExecuteAsync().ConfigureAwait(false);
                return silent.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                var interactive = await _pca.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync().ConfigureAwait(false);
                _account = interactive.Account;
                return interactive.AccessToken;
            }
        }
        catch
        {
            return null;
        }
    }

    private static string GetRedirectUri(string clientId)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // WAM broker redirect
            return $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}";
        }

        // Use loopback for desktop on macOS
        return "http://localhost";
    }
}
