# Authenticator Tray 🔐

A small Windows system tray app that shows your TOTP two-factor codes, built with WinForms on .NET 9.

> *Because I was tired of reaching for my phone.*

Left-click the tray icon and every code is right there, ticking down. Click one and it's on your clipboard.

## Features

- **Tray popup with live codes** - all accounts at a glance, refreshed continuously, with a shared countdown that turns orange at 10s and red at 5s.
- **Click to copy** - click a card (or its 📋 button) to copy the current code.
- **Folders** - group accounts under collapsible folder headers; the collapsed state is remembered.
- **Multi-column layout** - once the list would grow past 80% of the screen height, the popup spreads into balanced columns instead of scrolling forever.
- **Per-monitor DPI aware** - layout scales with the display, so text stays crisp on high-DPI screens.
- **Right-click a card** to move it to a folder (including a new one) or remove it.
- **Settings window** (⚙️ in the popup header):
  - add an account by scanning a QR code image (`otpauth://` URL), with all fields editable before saving, or by entering the details manually
  - create, rename and delete folders, assign accounts to folders, remove accounts
- **Single instance** - launching it twice just tells you it's already running.
- **Algorithms** - SHA1, SHA256 and SHA512; 6-8 digits; 30-second period.

Right-click the tray icon to exit.

## Requirements

- Windows
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) to build
- Python 3.6+ (optional, only for the [import scripts](migrate-codes/README.md))

## Build

`accounts.json` is embedded into the executable at build time, so it must exist before building. Create it from the example (or with `{ "accounts": [] }`):

```powershell
copy accounts.json.example accounts.json
```

Debug/Release build:

```powershell
dotnet build -c Release
```

Self-contained single-file executable:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

The result is `bin\Release\net9.0-windows\win-x64\publish\AuthenticatorTray.exe`. Drop it anywhere, and add it to your Startup folder if you want it running at logon.

## Accounts

### Where `accounts.json` is loaded from

1. `accounts.json` in the same folder as the executable.
2. If that file doesn't exist, the copy embedded in the exe at build time.

Changes made in the app (adding, removing, moving, folders) are saved to `accounts.json` next to the executable. If that file exists but can't be parsed, the app falls back to the embedded copy in read-only mode and refuses to save, so a broken file is never overwritten.

### Format

```json
{
  "accounts": [
    {
      "name": "GitHub (me@example.com)",
      "secret": "JBSWY3DPEHPK3PXP",
      "digits": 6,
      "algorithm": "SHA1",
      "folder": "Work"
    },
    {
      "name": "Blizzard (mysupercoolusername)",
      "secret": "JBSWY3DPEHPK3PXP",
      "digits": 6,
      "algorithm": "SHA1"
    }
  ],
  "folders": ["Work", "Games"],
  "collapsedFolders": ["Games"]
}
```

| Field | Description |
| --- | --- |
| `name` | Display name; must be unique. |
| `secret` | Base32 secret key. |
| `digits` | Code length, 6-8 (default 6). |
| `algorithm` | `SHA1` (default), `SHA256` or `SHA512`. |
| `folder` | Optional folder name. Folders referenced here are created automatically. |
| `folders` | Optional; folder display order. |
| `collapsedFolders` | Optional; folders shown collapsed in the popup. |

## Importing from Google Authenticator

Google Authenticator's export QR code (`otpauth-migration://`) can be converted into `accounts.json` with the Python scripts in [`migrate-codes/`](migrate-codes/README.md); see its README for step-by-step usage.

Regular single-account QR codes (`otpauth://`) can be added directly in the app via Settings.

## Project structure

```
Program.cs                 Entry point: single-instance mutex, DPI mode, tray icon
Forms/
  TrayPopup.cs             The popup: cards, folder headers, columns, countdown, context menu
  SettingsForm.cs          Add accounts, manage folders and accounts
  InputForm.cs             Small text prompt (folder names)
  RoundedForm.cs           Borderless rounded-corner base form
  ModernPopupForm.cs       Popup window base
Controls/MacCard.cs        Rounded account card
Services/
  AccountStore.cs          Load/save accounts.json (file or embedded fallback)
  TotpService.cs           Code generation via Otp.NET
  QrCodeService.cs         QR decoding (ZXing.Net) and otpauth:// parsing
Models/                    Account, AccountData, and the JSON shapes
Helpers/
  ColumnLayout.cs          Balanced multi-column packing
  UiScale.cs               DPI-aware sizing and fonts
  GraphicsExtensions.cs    Rounded-rectangle drawing
migrate-codes/             Google Authenticator import scripts (Python)
```

## Security

This is a convenience tool for personal use, not a hardened vault.

- `accounts.json` stores secrets in **plain text**. It is gitignored; keep it that way.
- The build embeds `accounts.json` into the executable as a fallback, so **the exe contains your secrets**. Don't share or publish it.
- Anyone with access to your Windows account can read your codes. Keeping 2FA on the same machine as your passwords is a trade-off you should make knowingly.
