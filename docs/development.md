# Building and checking the project

Run commands from the repository root in Windows PowerShell. The UI targets
.NET Framework 4.8, while the MCP server and C# tests target .NET 8.

## Requirements

- Windows 10/11 x64.
- .NET 9 SDK for builds and .NET 8 runtime for the MCP server and tests.
  Installing the .NET 8 SDK also supplies that runtime.
- .NET Framework 4.8 and Edge WebView2 Runtime to run the desktop app.
- Python 3.12 for the optional puzzle tooling and its tests.

NuGet supplies the .NET Framework reference assemblies, so a separate Developer
Pack is not required for command-line builds. `dotnet --info` shows installed
SDKs and runtimes. Recognition tests use the Windows native OpenCV package.

## Build and C# tests

```powershell
dotnet restore EpicRPGBotCSharp.sln
dotnet build EpicRPGBotCSharp.sln -c Release --no-restore
dotnet build tools/PuzzleReplay/PuzzleReplay.csproj -c Release
dotnet test EpicRPGBot.Tests/EpicRPGBot.Tests.csproj -c Release --no-build --no-restore
```

The build performs C# type checking. The repository has no separate lint command.
These commands do not launch the UI or send Discord commands.

Seven image regression tests require optional local datasets under ignored
`artifacts/`. Each reports an explicit skip when its required fixture is absent.
Providing the fixtures enables those tests automatically. A malformed fixture
that exists still fails its test. All shipped-model, synthetic-image and
non-image tests run without those datasets. See [puzzle validation](puzzle-validation.md).

If a running app or MCP process locks a build output, use a separate output tree:

```powershell
dotnet build EpicRPGBotCSharp.sln -c Release --artifacts-path artifacts/isolated-build
```

Some source-inspection tests expect the standard `bin/Release/net8.0` layout;
run the full C# test suite with the standard output paths shown above.

## Python tests

Create an isolated environment and install the pinned tooling dependencies:

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r tools/puzzle/requirements.txt
.\.venv\Scripts\python.exe -m unittest discover -s tools/puzzle/tests -v
```

The Python tests create synthetic fixtures and do not require Discord access or
the local image datasets. Python tooling is not needed to run the desktop app.

## Continuous integration

GitHub Actions runs Release builds, the replay build, all C# tests and the Python
tests on a Windows runner for pushes and pull requests. The runner installs both
SDK versions and Python dependencies. C# TRX results are retained as an artifact,
including on failure. CI never launches the desktop app.
