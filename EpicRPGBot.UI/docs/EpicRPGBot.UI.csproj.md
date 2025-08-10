# EpicRPGBot.UI.csproj

Overview
- WPF desktop app targeting .NET Framework 4.8.
- Uses WebView2 (Evergreen runtime) for embedding Discord.
- Builds as x64 with a persistent WebView2 user data folder.

Key Properties
- TargetFramework: net48
- UseWPF: true
- PlatformTarget: x64
- RuntimeIdentifier: win-x64
- CopyLocalLockFileAssemblies: true (ensures referenced libs are copied locally)
- PackageReference: Microsoft.Web.WebView2 (1.0.2792.45), Microsoft.NETFramework.ReferenceAssemblies (1.0.3)

Notes
- Build and run:
  - dotnet build EpicRPGBotCSharp.sln -c Debug
  - dotnet run --project EpicRPGBot.UI -c Debug
- Requires WebView2 Evergreen Runtime installed on Windows.