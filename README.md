# SmartRewrite

SmartRewrite is a Windows desktop background assistant built with C# and .NET 8. It watches for selected text, lets the user explicitly invoke `SmartWrite` from a right-click style action, calls the OpenAI Responses API for 3 professional rewrites, and replaces the selected text after the user confirms a choice.

## Current user flow

1. Start the app. It runs in the Windows tray.
2. Select text in a supported app such as Notepad.
3. Right-click the selected text.
4. Click the SmartWrite launcher popup.
5. Wait for 3 professional variations.
6. Select one variation.
7. Click `Confirm`.
8. The selected text is replaced.

## Project structure

- `SmartRewrite.sln`: Visual Studio solution file.
- `SmartRewrite.App/`: WPF desktop application.
- `installer/SmartRewrite.iss`: Inno Setup installer script.

## Security and API key handling

This repository does not store your OpenAI API key in source control.

The app reads the API key from the Windows user environment variable:

`OPENAI_API_KEY`

Set it on each machine locally before running the app.

PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "your_api_key_here", "User")
```

Then close and reopen PowerShell or restart the app so the new environment variable is visible.

Official OpenAI references:

- [Responses API](https://platform.openai.com/docs/api-reference/responses?lang=csharp)
- [Authentication](https://platform.openai.com/docs/api-reference/authentication?api-mode=responses)

## Requirements

- Windows 11 or a recent Windows version with desktop support
- .NET 8 SDK
- Optional: Inno Setup if you want to build the installer

## Build on another computer

Clone the repository, then run:

```powershell
dotnet restore .\SmartRewrite.sln
dotnet build .\SmartRewrite.sln -c Release
```

If you want a self-contained app publish:

```powershell
dotnet publish .\SmartRewrite.App\SmartRewrite.App.csproj -c Release -r win-x64 --self-contained true
```

## Run locally

From the project root:

```powershell
dotnet run --project .\SmartRewrite.App\SmartRewrite.App.csproj
```

Or run the built executable from:

`SmartRewrite.App\bin\Release\net8.0-windows\`

## Build the installer

1. Build or publish the app first.
2. Install [Inno Setup](https://jrsoftware.org/isinfo.php).
3. Open `installer/SmartRewrite.iss`.
4. Build the installer.

The installer script can register the app to start automatically when the user signs in.

## Notes

- The app uses clipboard capture and Windows input automation, so behavior depends on the target app.
- Standard editors like Notepad are the easiest place to test first.
- Some secure apps, console windows, remote sessions, and custom editors may behave differently.
- `appsettings.json` contains non-sensitive defaults such as model name, endpoint, and timing values. It does not contain an API key.

## Recommended files to commit

Commit the source files, solution file, installer script, and README. Do not commit:

- `bin/`
- `obj/`
- local publish folders
- `.dotnet/`
- any file that contains real secrets
