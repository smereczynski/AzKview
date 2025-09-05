using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AzKview.App.ViewModels;
using AzKview.App.Views;
using AzKview.App.Services;
using AzKview.Core.Services;

namespace AzKview.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var clientId = Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID") ?? "YOUR_CLIENT_ID";
            var tenantId = Environment.GetEnvironmentVariable("AZURE_AD_TENANT_ID") ?? "common";
            var scopesEnv = Environment.GetEnvironmentVariable("AZURE_AD_SCOPES");
            var defaultScopes = string.IsNullOrWhiteSpace(scopesEnv)
                ? new[] { "User.Read" }
                : scopesEnv
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();

            IAuthService authService = new AuthService(clientId, tenantId, defaultScopes);
            // Key Vault configuration: no built-in default. Provide AZURE_KEY_VAULT_URI in your environment (e.g., via .env.local).
            // Example: AZURE_KEY_VAULT_URI=https://kvdevplc.vault.azure.net/
            var vaultUriEnv = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");
            MainWindowViewModel vm;
            if (!string.IsNullOrWhiteSpace(vaultUriEnv))
            {
                var vaultUri = new Uri(vaultUriEnv);
                var credential = new KeyVaultCredential(authService);
                var kvService = new KeyVaultService(vaultUri, credential);
                vm = new MainWindowViewModel(new TimeService(), authService, kvService);
            }
            else
            {
                // If not set, Key Vault features will remain inactive (NullKeyVaultService in VM).
                vm = new MainWindowViewModel(new TimeService(), authService);
            }

            desktop.MainWindow = new MainWindow { DataContext = vm };
            // Fire-and-forget init to refresh KV on startup (handles auth silently or interactively when needed)
            _ = vm.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}