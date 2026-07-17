# Repository Guidelines

## Project Structure & Module Organization
This is a .NET 8 Windows Forms kiosk application. The solution file is `KioskWin.sln`. Application code lives in `src/KioskWin`, with core logic in `Core/`, WinForms UI in `Forms/`, and startup/configuration in `Program.cs` and `appsettings.json`. Unit tests live in `tests/KioskWin.Tests` and reference the main project. Packaging helpers are at the repository root (`build.bat`) and in the app project (`install-shortcut.ps1`).

## Build, Test, and Development Commands
- `dotnet restore KioskWin.sln`: restores NuGet packages.
- `dotnet build KioskWin.sln -c Debug`: builds the app and tests for local development.
- `dotnet test KioskWin.sln -c Release --no-restore --nologo`: runs the xUnit test suite.
- `build.bat`: restores, tests, publishes a self-contained `win-x64` build, and creates `KioskWin-win-x64.zip`.
- `build.bat notest`: publishes and packages while skipping tests.

Because the projects target `net8.0-windows` and WinForms, use Windows with the .NET SDK for normal builds and packaging.

## Coding Style & Naming Conventions
Use C# with nullable reference types and implicit usings enabled. Keep the existing 4-space indentation and file-scoped namespace style. Name types and public members with `PascalCase`, locals and parameters with `camelCase`, and constants with `PascalCase` unless an existing interop name requires otherwise. Keep UI code in `Forms/`; place testable non-UI behavior in `Core/`.

## Testing Guidelines
Tests use xUnit. Add focused tests under `tests/KioskWin.Tests` and name files after the unit under test, such as `RetryControllerTests.cs`. Prefer deterministic tests for parsers, configuration, retry behavior, logging, and password hashing. Run `dotnet test KioskWin.sln -c Release` before submitting changes.

## Commit & Pull Request Guidelines
Recent history uses Conventional Commit-style prefixes, especially `feat:` and `fix:` with concise summaries, for example `fix: attach console in --hash-password CLI mode`. Keep commits scoped to one change. Pull requests should describe the behavior change, list verification commands run, link related issues when available, and include screenshots or notes for visible WinForms changes.

## Security & Configuration Tips
Do not commit real kiosk credentials. Generate admin password hashes with `KioskWin.exe --hash-password <password>` and store only the hash and salt in `appsettings.json`. Treat deployment output in `publish/` and generated zip files as build artifacts.
