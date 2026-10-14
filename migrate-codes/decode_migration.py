import argparse
import base64
import getpass
import json
import sys
import urllib.parse
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import auth_migration_pb2  # noqa: E402

ALGORITHMS = {1: "SHA1", 2: "SHA256", 3: "SHA512", 4: "MD5"}
# Google sends a DigitCount enum (1 = six, 2 = eight) rather than the digit count.
DIGITS = {1: 6, 2: 8}


def decode(url):
    if not url.startswith("otpauth-migration://"):
        raise ValueError("not an otpauth-migration:// URL")
    data = urllib.parse.parse_qs(urllib.parse.urlparse(url).query).get("data")
    if not data:
        raise ValueError("URL has no data parameter")

    payload = auth_migration_pb2.MigrationPayload()
    payload.ParseFromString(base64.b64decode(data[0]))

    accounts = []
    for otp in payload.otp_parameters:
        if otp.type == otp.OTP_HOTP:
            print(f"Skipping HOTP account {otp.name!r} (not supported)", file=sys.stderr)
            continue
        name = f"{otp.issuer} ({otp.name})" if otp.issuer else otp.name
        accounts.append({
            "name": name,
            "secret": base64.b32encode(otp.secret).decode().rstrip("="),
            "digits": DIGITS.get(otp.digits, otp.digits if otp.digits in (6, 7, 8) else 6),
            "algorithm": ALGORITHMS.get(otp.algorithm, "SHA1"),
        })
    return accounts


def main():
    parser = argparse.ArgumentParser(description="Decode a Google Authenticator otpauth-migration:// URL.")
    parser.add_argument("url", nargs="?", help="migration URL; omit to paste it at a hidden prompt (keeps it out of shell history)")
    parser.add_argument("-o", "--output", help="write the accounts as accounts.json-style JSON to this file instead of printing them")
    args = parser.parse_args()

    url = (args.url or getpass.getpass("Paste otpauth-migration:// URL: ")).strip()
    try:
        accounts = decode(url)
    except Exception as e:
        sys.exit(f"Could not decode URL: {e}")

    if args.output:
        out = Path(args.output)
        if out.exists():
            sys.exit(f"{out} already exists; refusing to overwrite")
        out.write_text(json.dumps({"accounts": accounts}, indent=2), encoding="utf-8")
        print(f"Wrote {len(accounts)} account(s) to {out}")
    else:
        print(json.dumps({"accounts": accounts}, indent=2))


if __name__ == "__main__":
    main()
