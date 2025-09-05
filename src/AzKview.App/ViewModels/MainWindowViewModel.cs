using AzKview.Core.Services;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AzKview.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    
    public MainWindowViewModel() : this(new TimeService(), null!) { }

    public MainWindowViewModel(ITimeService timeService, IAuthService authService)
    {
        _authService = authService ?? new NullAuthService();
        Greeting = $"Welcome to Avalonia! UTC: {timeService.UtcNow:O}";
        _authService.AuthenticationStateChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(AccountUpn));
        };
    }

    public string Greeting { get; }

    public bool IsAuthenticated => _authService.IsAuthenticated;
    public string? AccountUpn => _authService.AccountUpn;

    [RelayCommand]
    private async Task SignInAsync()
    {
        await _authService.SignInAsync();
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(AccountUpn));
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(AccountUpn));
    }
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
