# migrate-codes

Python helpers for moving accounts out of Google Authenticator and into Authenticator Tray's `accounts.json`.

Google Authenticator exports accounts as a QR code containing an `otpauth-migration://offline?data=...` URL. The `data` part is a base64-encoded protobuf (`auth_migration.proto`) listing every exported account. These scripts decode it.

| File | Purpose |
| --- | --- |
| `setup.py` | All-in-one: checks dependencies, generates the protobuf module, decodes `img.png` and writes `accounts.json`. |
| `decoder_qr.py` | Prints the URL(s) encoded in `img.png`. |
| `decode_migration.py` | Decodes one `otpauth-migration://` URL and prints each account. |
| `auth_migration.proto` | Protobuf schema of the migration payload. |

## 1. Prerequisites

- Python 3.6 or newer
- Packages:
  ```powershell
  pip install protobuf pyzbar Pillow
  ```
  (`setup.py` installs any that are missing.)
- `auth_migration_pb2.py`, generated from `auth_migration.proto`. `setup.py` generates it if `grpcio-tools` or `protoc` is available; otherwise:
  ```powershell
  pip install grpcio-tools
  cd migrate-codes
  python -m grpc_tools.protoc --python_out=. --proto_path=. auth_migration.proto
  ```
  The generated file is gitignored.

## 2. Export from Google Authenticator

1. On your phone, open Google Authenticator.
2. Open the menu and choose **Transfer accounts** > **Export accounts**.
3. Select the accounts to export and tap **Next**. A QR code is shown (large exports are split over several QR codes; handle them one at a time).
4. Take a screenshot of the QR code and copy it to your PC as `migrate-codes/img.png`.

## 3. Convert

### Option A: `setup.py` (recommended)

```powershell
python migrate-codes/setup.py
```

It can be run from any directory. It decodes `img.png` (plain `otpauth://` codes are accepted too) and writes the accounts to:

- `accounts.json` in the app's project root (the parent of `migrate-codes/`), or
- `updated_accounts.json` in the same place if `accounts.json` already exists, so nothing is overwritten.

### Option B: step by step

Run these from inside `migrate-codes/`, since they look for `img.png` and `auth_migration_pb2.py` in the current directory:

```powershell
cd migrate-codes
python decoder_qr.py
```

This prints the `otpauth-migration://...` URL. Paste it into the `url` variable at the top of `decode_migration.py`, then:

```powershell
python decode_migration.py
```

Each account is printed with its name, issuer, Base32 secret, digits and algorithm (`1` = SHA1, `2` = SHA256, `3` = SHA512). Copy the values into `accounts.json` by hand.

## 4. Merge into the app

The app reads `accounts.json` from the folder containing the executable, and the build embeds the project-root `accounts.json` as a fallback (see the [main README](../README.md#accounts)).

- **New setup:** the generated `accounts.json` in the project root is ready; build or publish the app.
- **Existing accounts:** copy the entries from the `accounts` array of `updated_accounts.json` into the `accounts` array of your `accounts.json`. Names must be unique. Optionally add a `folder` to each entry.
- **Already running a published exe:** edit the `accounts.json` next to the exe (or merge into it) and restart the app.

Delete `updated_accounts.json` once merged.

## 5. Clean up

> **Warning:** the export QR code contains the secrets for every exported account. Anyone with the image can generate your codes.

Once the accounts work in the app:

- delete `img.png` and the screenshot on your phone (and any cloud photo backup of it),
- delete any copies of the `otpauth-migration://` URL, including the one pasted into `decode_migration.py`,
- delete `updated_accounts.json` if you created one.
