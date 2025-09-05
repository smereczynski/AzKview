# AzKview

[![CI](https://github.com/smereczynski/AzKview/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/smereczynski/AzKview/actions/workflows/ci.yml)

Cross-platform Avalonia UI application (net9.0) using MVVM and clean solution structure.

## Structure
- `src/AzKview.App`: Desktop entry point (Avalonia App), references Core and UI.
- `src/AzKview.Core`: Domain/services.
- `src/AzKview.UI`: Reusable controls/views/resources.
- `tests/AzKview.Core.Tests`: xUnit tests for Core.

## Prerequisites
- .NET SDK 9.0+
- Avalonia templates (installed via `dotnet new install Avalonia.Templates`)

## Build & Run
- Build solution:
  - `dotnet build`
- Run app:
  - `dotnet run --project src/AzKview.App`
- Run tests:
  - `dotnet test`

## Git
This repo is structured for clean CI and PRs. Use feature branches and conventional commits.
