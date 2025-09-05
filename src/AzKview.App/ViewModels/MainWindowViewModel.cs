using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AzKview.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace AzKview.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly bool _hasKeyVault;
    
    public MainWindowViewModel() : this(new TimeService(), null!, null!) { }

    public MainWindowViewModel(ITimeService timeService, IAuthService authService)
        : this(timeService, authService, null!) { }

    public MainWindowViewModel(ITimeService timeService, IAuthService authService, IKeyVaultService keyVaultService)
    {
        _authService = authService ?? new NullAuthService();
    _keyVaultService = keyVaultService ?? new NullKeyVaultService();
    _hasKeyVault = _keyVaultService is not NullKeyVaultService;
        Greeting = $"Welcome to Avalonia! UTC: {timeService.UtcNow:O}";
        Secrets = new ObservableCollection<SecretItemViewModel>();
        _authService.AuthenticationStateChanged += (_, __) =>
            Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(IsAuthenticated));
                OnPropertyChanged(nameof(AccountUpn));
                (SignInCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (SignOutCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (RefreshSecretsCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                foreach (var s in Secrets!) s.RefreshCanExecute();
            });
    }

    public string Greeting { get; }

    public bool IsAuthenticated => _authService.IsAuthenticated;
    public string? AccountUpn => _authService.AccountUpn;

    [ObservableProperty]
    private bool isWriteMode;

    partial void OnIsWriteModeChanged(bool value)
    {
        foreach (var s in Secrets)
            s.RefreshCanExecute();
    }

    [RelayCommand]
    private void SetReadMode() => IsWriteMode = false;

    [RelayCommand]
    private void SetWriteMode() => IsWriteMode = true;

    public ObservableCollection<SecretItemViewModel> Secrets { get; }

    public async Task InitializeAsync()
    {
        // Prompt for sign-in if needed, then refresh secrets on startup.
        if (!_authService.IsAuthenticated)
        {
            await _authService.SignInAsync();
        }
        if (_hasKeyVault && _authService.IsAuthenticated)
        {
            await RefreshSecretsAsync();
        }
    }

    private bool CanSignIn() => !_authService.IsAuthenticated;

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        await _authService.SignInAsync();
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(AccountUpn));
            (SignInCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (SignOutCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (RefreshSecretsCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        });
    }

    private bool CanSignOut() => _authService.IsAuthenticated;

    [RelayCommand(CanExecute = nameof(CanSignOut))]
    private async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(AccountUpn));
            (SignInCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (SignOutCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (RefreshSecretsCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        });
    }

    private bool CanRefreshSecrets() => _authService.IsAuthenticated;

    [RelayCommand(CanExecute = nameof(CanRefreshSecrets))]
    private async Task RefreshSecretsAsync()
    {
        try
        {
            var items = await _keyVaultService.ListSecretsAsync();
            Dispatcher.UIThread.Post(() =>
            {
                Secrets.Clear();
                foreach (var s in items.OrderBy(s => s.Name))
                {
                    Secrets.Add(new SecretItemViewModel(
                        s.Name,
                        s.ContentType,
                        s.Enabled,
                        s.UpdatedOn,
                        s.NotBefore,
                        s.ExpiresOn,
                        s.Tags,
                        _keyVaultService,
                        () => IsWriteMode && IsAuthenticated));
                }
            });
        }
        catch (Exception ex)
        {
            // Avoid crashing the UI if RBAC/consent is missing; surface in console for now.
            Console.WriteLine($"[KeyVault] Refresh failed: {ex}");
        }
    }

    private bool CanEditSecret(object? parameter)
        => IsWriteMode && _authService.IsAuthenticated && parameter is SecretItemViewModel;

        // Edit command moved to SecretItemViewModel for simpler XAML bindings.
}

file sealed class NullAuthService : IAuthService
{
    public string? AccountUpn => null;
    public bool IsAuthenticated => false;
    public event EventHandler<bool>? AuthenticationStateChanged;
    public Task<string?> AcquireTokenAsync(string[] scopes) => Task.FromResult<string?>(null);
    public Task<bool> SignInAsync()
    {
        // Notify listeners even though auth state doesn't change in the null implementation.
        AuthenticationStateChanged?.Invoke(this, IsAuthenticated);
        return Task.FromResult(false);
    }
    public Task SignOutAsync()
    {
        // Notify listeners to indicate an attempted sign-out occurred.
        AuthenticationStateChanged?.Invoke(this, IsAuthenticated);
        return Task.CompletedTask;
    }
}

    public sealed partial class SecretItemViewModel : ObservableObject
{
    private readonly IKeyVaultService _service;
    private readonly Func<bool>? _canWrite;
    public string Name { get; }
    public string? ContentType { get; }
    public bool? Enabled { get; }
    public DateTimeOffset? UpdatedOn { get; }
        public DateTimeOffset? NotBefore { get; }
        public DateTimeOffset? ExpiresOn { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string>? Tags { get; }
    // Parent write mode can be supplied later if needed.

    [ObservableProperty]
    private string? value;

    [ObservableProperty]
    private bool isRevealed;

    [ObservableProperty]
    private bool isEditing;

    public bool IsMasked => !IsRevealed;
    partial void OnIsRevealedChanged(bool value) => OnPropertyChanged(nameof(IsMasked));
    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowEdit));
        OnPropertyChanged(nameof(CanShowSave));
    }

        public SecretItemViewModel(
            string name,
            string? contentType,
            bool? enabled,
            DateTimeOffset? updatedOn,
            DateTimeOffset? notBefore,
            DateTimeOffset? expiresOn,
            System.Collections.Generic.IReadOnlyDictionary<string, string>? tags,
            IKeyVaultService service,
            Func<bool>? canWrite = null)
    {
        Name = name;
        ContentType = contentType;
        Enabled = enabled;
        UpdatedOn = updatedOn;
            NotBefore = notBefore;
            ExpiresOn = expiresOn;
            Tags = tags;
        _service = service;
        _canWrite = canWrite;
    }

    public async Task RevealAsync()
    {
        if (IsRevealed)
        {
            // Hide
            IsRevealed = false;
            return;
        }
        // Reveal
        Value = await _service.GetSecretValueAsync(Name);
        IsRevealed = true;
    }

    [RelayCommand]
    private Task Reveal() => RevealAsync();

    private bool CanEdit() => _canWrite?.Invoke() == true;

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task Edit()
    {
        if (!IsEditing)
        {
            if (Value is null)
            {
                // Fetch current value first if not revealed yet
                Value = await _service.GetSecretValueAsync(Name);
                if (Value is null) return;
            }
            IsRevealed = true;
            IsEditing = true;
            return;
        }

        // Save
        var toSave = Value ?? string.Empty;
        await _service.SetSecretValueAsync(Name, toSave, ContentType);
        IsEditing = false;
    }

    public bool CanWrite => _canWrite?.Invoke() == true;
    public bool CanShowEdit => CanWrite && !IsEditing;
    public bool CanShowSave => CanWrite && IsEditing;

    public void RefreshCanExecute()
    {
        (EditCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanWrite));
        OnPropertyChanged(nameof(CanShowEdit));
        OnPropertyChanged(nameof(CanShowSave));
    }
}

file sealed class NullKeyVaultService : IKeyVaultService
{
    public Task<string?> GetSecretValueAsync(string name, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    public Task<System.Collections.Generic.IReadOnlyList<KeyVaultSecretInfo>> ListSecretsAsync(System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult<System.Collections.Generic.IReadOnlyList<KeyVaultSecretInfo>>(Array.Empty<KeyVaultSecretInfo>());
    public Task<string> SetSecretValueAsync(string name, string value, string? contentType = null, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult("");
}
