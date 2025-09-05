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
            var defaultScopes = new[] { "User.Read" };

            IAuthService authService = new AuthService(clientId, tenantId, defaultScopes);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new TimeService(), authService),
            };
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