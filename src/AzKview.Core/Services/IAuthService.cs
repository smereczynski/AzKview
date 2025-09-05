using System;
using System.Threading.Tasks;

namespace AzKview.Core.Services;

public interface IAuthService
{
    string? AccountUpn { get; }
    bool IsAuthenticated { get; }

    Task<bool> SignInAsync();
    Task SignOutAsync();
    Task<string?> AcquireTokenAsync(string[] scopes);

    event EventHandler<bool>? AuthenticationStateChanged;
}
