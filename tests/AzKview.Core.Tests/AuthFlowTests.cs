using System;
using System.Threading.Tasks;
using AzKview.App.ViewModels;
using AzKview.Core.Services;
using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace AzKview.Core.Tests;

public class AuthFlowTests
{
    private sealed class FakeAuthService : IAuthService
    {
        public string? AccountUpn { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public event EventHandler<bool>? AuthenticationStateChanged;

        public Task<string?> AcquireTokenAsync(string[] scopes)
            => Task.FromResult<string?>(IsAuthenticated ? "token" : null);

        public Task<bool> SignInAsync()
        {
            IsAuthenticated = true;
            AccountUpn = "user@example.com";
            AuthenticationStateChanged?.Invoke(this, true);
            return Task.FromResult(true);
        }

        public Task SignOutAsync()
        {
            IsAuthenticated = false;
            AccountUpn = null;
            AuthenticationStateChanged?.Invoke(this, false);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingAuthService : IAuthService
    {
        public string? AccountUpn => null;
        public bool IsAuthenticated => false;
        public event EventHandler<bool>? AuthenticationStateChanged;
        public Task<string?> AcquireTokenAsync(string[] scopes) => Task.FromResult<string?>(null);
        public Task<bool> SignInAsync()
        {
            AuthenticationStateChanged?.Invoke(this, false);
            return Task.FromResult(false);
        }
        public Task SignOutAsync()
        {
            AuthenticationStateChanged?.Invoke(this, false);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAuthService : IAuthService
    {
        public string? AccountUpn => null;
        public bool IsAuthenticated => false;
        public event EventHandler<bool>? AuthenticationStateChanged;
        public Task<string?> AcquireTokenAsync(string[] scopes)
        {
            AuthenticationStateChanged?.Invoke(this, false);
            throw new InvalidOperationException("Acquire failed");
        }
        public Task<bool> SignInAsync()
        {
            AuthenticationStateChanged?.Invoke(this, false);
            throw new InvalidOperationException("Sign-in failed");
        }
        public Task SignOutAsync()
        {
            AuthenticationStateChanged?.Invoke(this, false);
            throw new InvalidOperationException("Sign-out failed");
        }
    }

    private sealed class FixedTimeService : ITimeService
    {
        public DateTime UtcNow { get; }
        public FixedTimeService(DateTime utcNow) => UtcNow = utcNow;
    }

    [Fact]
    public async Task SignIn_TogglesStateAndCommands()
    {
        var auth = new FakeAuthService();
        var vm = new MainWindowViewModel(new FixedTimeService(DateTime.UnixEpoch), auth);

        Assert.False(vm.IsAuthenticated);
        Assert.Null(vm.AccountUpn);
        Assert.True(((IRelayCommand)vm.SignInCommand).CanExecute(null));
        Assert.False(((IRelayCommand)vm.SignOutCommand).CanExecute(null));

        await vm.SignInCommand.ExecuteAsync(null);

        Assert.True(vm.IsAuthenticated);
        Assert.Equal("user@example.com", vm.AccountUpn);
        Assert.False(((IRelayCommand)vm.SignInCommand).CanExecute(null));
        Assert.True(((IRelayCommand)vm.SignOutCommand).CanExecute(null));
    }

    [Fact]
    public async Task SignOut_TogglesStateAndCommands()
    {
        var auth = new FakeAuthService();
        var vm = new MainWindowViewModel(new FixedTimeService(DateTime.UnixEpoch), auth);

        // sign in first
        await vm.SignInCommand.ExecuteAsync(null);
        Assert.True(vm.IsAuthenticated);

        await vm.SignOutCommand.ExecuteAsync(null);

        Assert.False(vm.IsAuthenticated);
        Assert.Null(vm.AccountUpn);
        Assert.True(((IRelayCommand)vm.SignInCommand).CanExecute(null));
        Assert.False(((IRelayCommand)vm.SignOutCommand).CanExecute(null));
    }

    [Fact]
    public async Task AuthService_Event_RefreshesBindings()
    {
        var auth = new FakeAuthService();
        var vm = new MainWindowViewModel(new FixedTimeService(DateTime.UnixEpoch), auth);

        // Raise event externally (simulating background token refresh)
        await auth.SignInAsync();

        Assert.True(vm.IsAuthenticated);
        Assert.Equal("user@example.com", vm.AccountUpn);
        Assert.False(((IRelayCommand)vm.SignInCommand).CanExecute(null));
        Assert.True(((IRelayCommand)vm.SignOutCommand).CanExecute(null));
    }

    [Fact]
    public async Task AcquireToken_ReturnsNull_WhenNotSignedIn()
    {
        var auth = new FakeAuthService();
        var token = await auth.AcquireTokenAsync(new[] { "User.Read" });
        Assert.Null(token);
    }

    [Fact]
    public async Task AcquireToken_ReturnsToken_WhenSignedIn()
    {
        var auth = new FakeAuthService();
        await auth.SignInAsync();
        var token = await auth.AcquireTokenAsync(new[] { "User.Read" });
        Assert.Equal("token", token);
    }

    [Fact]
    public async Task SignIn_FailingAuth_LeavesStateUnauthenticated()
    {
        var auth = new FailingAuthService();
        var vm = new MainWindowViewModel(new FixedTimeService(DateTime.UnixEpoch), auth);
        var beforeCanSignIn = ((IRelayCommand)vm.SignInCommand).CanExecute(null);
        Assert.True(beforeCanSignIn);

        await vm.SignInCommand.ExecuteAsync(null);

        Assert.False(vm.IsAuthenticated);
        Assert.Null(vm.AccountUpn);
        Assert.True(((IRelayCommand)vm.SignInCommand).CanExecute(null));
        Assert.False(((IRelayCommand)vm.SignOutCommand).CanExecute(null));
    }

    [Fact]
    public async Task SignIn_Throws_PropagatesException()
    {
        var auth = new ThrowingAuthService();
        var vm = new MainWindowViewModel(new FixedTimeService(DateTime.UnixEpoch), auth);
        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.SignInCommand.ExecuteAsync(null));
    }

    [Fact]
    public async Task SignOut_Throws_PropagatesException()
    {
        var auth = new ThrowingAuthService();
        var vm = new MainWindowViewModel(new FixedTimeService(DateTime.UnixEpoch), auth);
        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.SignOutCommand.ExecuteAsync(null));
    }

    [Fact]
    public async Task AcquireToken_Throws_PropagatesException()
    {
        var auth = new ThrowingAuthService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.AcquireTokenAsync(new[] { "User.Read" }));
    }
}
