# AzKview

[![CI](https://github.com/smereczynski/AzKview/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/smereczynski/AzKview/actions/workflows/ci.yml)

Cross‑platform desktop app built with Avalonia UI (net9.0), MVVM, and a clean multi‑project layout. It includes Azure AD authentication via MSAL.NET with platform‑aware UX (Windows WAM, macOS system web session).

## Project map

Top‑level layout:

- `src/`
  - `AzKview.App/` — Desktop application (Avalonia)
    - `App.axaml` / `App.axaml.cs` — App resources/bootstrap; reads env vars and wires services
    - `Program.cs` — Avalonia bootstrapping (UsePlatformDetect, Inter font, logging)
    - `Services/`
  - `AuthService.cs` — MSAL.NET auth: token cache, Windows WAM, macOS system web, logging
  - `KeyVaultService.cs` — Azure Key Vault secrets (RBAC via Azure SDK)
  - `KeyVaultCredential.cs` — Bridges MSAL tokens to Azure SDK TokenCredential
    - `ViewModels/`
  - `MainWindowViewModel.cs` — Greeting, Sign in/out; Key Vault list/reveal/edit with READ/WRITE modes
      - `ViewModelBase.cs` — ObservableObject base
    - `Views/`
      - `MainWindow.axaml` / `.cs` — Main UI with Sign in/out and UPN display
    - `ViewLocator.cs` — ViewModel→View resolution by naming convention
  - `AzKview.Core/` — Core abstractions and services (UI‑free)
    - `Services/`
      - `IAuthService.cs` — Auth abstraction (UPN, state, events, token acquisition)
  - `IKeyVaultService.cs` — Key Vault abstraction (list, get, set secrets)
      - `TimeService.cs` — Time provider (UTC now) for testability
  - `AzKview.UI/` — Placeholder for reusable UI components
- `tests/`
  - `AzKview.Core.Tests/` — xUnit tests (TimeService)

## Authentication

MSAL.NET Public Client Application configured at runtime:

- Windows: WAM broker for native account picker and SSO, redirect URI:
  - `ms-appx-web://microsoft.aad.brokerplugin/{CLIENT_ID}`
- macOS: System web authentication session (ASWebAuthenticationSession), redirect URI:
  - `http://localhost`

Token cache is persisted securely via `Microsoft.Identity.Client.Extensions.Msal` (Keychain/Keyring).

### Env vars

Set before launching the app:

- `AZURE_AD_CLIENT_ID` — App registration (application) ID
- `AZURE_AD_TENANT_ID` — Tenant ID or alias (e.g., `common`)
- `AZURE_AD_SCOPES` — Space/comma/semicolon‑separated scopes (default: `User.Read`)
- `AZURE_AD_LOG_MSAL` — `true` to enable verbose MSAL logs to console (default: `false`)
- `AZURE_AD_LOG_PII` — `true` to include PII in logs (default: `false`; use only for local debugging)

Example (macOS zsh):

```sh
export AZURE_AD_CLIENT_ID="<app-id>"
export AZURE_AD_TENANT_ID="<tenant-id-or-common>"
export AZURE_AD_SCOPES="User.Read offline_access openid profile"
export AZURE_AD_LOG_MSAL=true
export AZURE_AD_LOG_PII=false
dotnet run --project src/AzKview.App/AzKview.App.csproj
```

## Key Vault (RBAC)

The app integrates with Azure Key Vault for secrets using RBAC and the Azure SDK. It uses your interactive sign-in (no access keys).

Set this environment variable before launching the app. There’s no built-in default; if not set, Key Vault features are inactive.

- `AZURE_KEY_VAULT_URI` — Your vault URI, e.g. `https://kv.vault.azure.net/`

Example (macOS zsh, using the provided test vault):

```sh
export AZURE_KEY_VAULT_URI="https://kv.vault.azure.net/"
dotnet run --project src/AzKview.App
```

Features:
- READ mode: List secrets (metadata) and “Reveal” value on demand.
- WRITE mode: Toggle “Write mode” to enable “Edit” per-secret (updates value, creates a new version).

RBAC Required Roles:
- Read: Key Vault Secrets User
- Write: Key Vault Secrets Officer (or higher)

## Build, run, and test

```sh
# Build entire solution
dotnet build

# Run the desktop app
dotnet run --project src/AzKview.App

# Run unit tests
dotnet test
```

## CI

GitHub Actions builds and tests on Windows and macOS. Workflows are under `.github/workflows` and are permitted via `.gitignore` whitelist.

## Troubleshooting

- Sign in button disabled
  - The button enablement is driven by command CanExecute; ensure the app started and ViewModel loaded.
- Browser opens but sign in fails on macOS
  - Confirm the app registration has the `http://localhost` redirect URI.
- WAM broker fails on Windows
  - Confirm the broker redirect URI is configured and the broker (WAM) is available on the machine.
- Need more auth diagnostics
  - Set `AZURE_AD_LOG_MSAL=true` (and optionally `AZURE_AD_LOG_PII=true`) before launch.

## Contributing

Use feature branches and conventional commits. Open PRs against `main`. The repository enforces CI and simple test coverage.
