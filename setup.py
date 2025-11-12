#!/usr/bin/env python3
"""
Setup script for 2FA Authenticator Tray
This script handles dependencies, protobuf generation, and file setup.
It does NOT run the tray applications - you'll do that manually.
"""

import os
import sys
import subprocess
import json
import shutil
import urllib.parse
import base64
import re
from pathlib import Path

# Project paths
PROJECT_ROOT = Path(__file__).parent
PROTO_FILE = PROJECT_ROOT / "auth_migration.proto"
PROTO_PY_FILE = PROJECT_ROOT / "auth_migration_pb2.py"
CSHARP_TRAY_DIR = PROJECT_ROOT / "csharp-tray" / "AuthenticatorTray"
CSHARP_ACCOUNTS_JSON = CSHARP_TRAY_DIR / "accounts.json"
CSHARP_ACCOUNTS_EXAMPLE = CSHARP_TRAY_DIR / "accounts.json.example"
IMG_PNG = PROJECT_ROOT / "img.png"

# Required Python packages (for QR decoding and protobuf)
REQUIRED_PACKAGES = [
    "protobuf",
    "pyzbar",
    "Pillow",
]

def check_python_version():
    """Check if Python version is 3.6+"""
    if sys.version_info < (3, 6):
        print("❌ Python 3.6 or higher is required!")
        sys.exit(1)
    print(f"✅ Python {sys.version_info.major}.{sys.version_info.minor} detected")

def install_dependencies():
    """Install required Python packages"""
    print("\n📦 Checking and installing dependencies...")
    missing = []
    for package in REQUIRED_PACKAGES:
        try:
            __import__(package.replace("-", "_"))
            print(f"  ✅ {package} is installed")
        except ImportError:
            missing.append(package)
            print(f"  ❌ {package} is missing")
    
    if missing:
        print(f"\n🔧 Installing missing packages: {', '.join(missing)}")
        for package in missing:
            subprocess.check_call([sys.executable, "-m", "pip", "install", package])
        print("✅ All dependencies installed!")
    else:
        print("✅ All dependencies are already installed!")

def generate_protobuf():
    """Generate auth_migration_pb2.py from auth_migration.proto"""
    print("\n🔨 Checking protobuf file...")
    
    if not PROTO_FILE.exists():
        print(f"❌ {PROTO_FILE} not found!")
        return False
    
    # Check if generated file exists and is newer than proto file
    if PROTO_PY_FILE.exists():
        if PROTO_PY_FILE.stat().st_mtime > PROTO_FILE.stat().st_mtime:
            print("✅ Protobuf file is up to date")
            return True
    
    print("🔧 Generating auth_migration_pb2.py from auth_migration.proto...")
    try:
        subprocess.check_call([
            sys.executable, "-m", "grpc_tools.protoc",
            f"--python_out={PROJECT_ROOT}",
            f"--proto_path={PROJECT_ROOT}",
            str(PROTO_FILE)
        ])
        print("✅ Protobuf file generated successfully!")
        return True
    except subprocess.CalledProcessError:
        print("⚠️  grpc_tools.protoc failed, trying alternative method...")
        try:
            # Alternative: use protoc directly if available
            subprocess.check_call([
                "protoc",
                f"--python_out={PROJECT_ROOT}",
                f"--proto_path={PROJECT_ROOT}",
                str(PROTO_FILE)
            ])
            print("✅ Protobuf file generated successfully!")
            return True
        except (subprocess.CalledProcessError, FileNotFoundError):
            print("⚠️  Could not generate protobuf file automatically.")
            print("   If auth_migration_pb2.py already exists, you can continue.")
            print("   Otherwise, install protoc and run:")
            print(f"   protoc --python_out=. --proto_path=. {PROTO_FILE.name}")
            return PROTO_PY_FILE.exists()

def parse_otpauth_url(url):
    """Parse an otpauth:// URL and extract account information"""
    try:
        parsed = urllib.parse.urlparse(url)
        if parsed.scheme != "otpauth":
            return None
        
        # Extract label (issuer:account or just account)
        label = urllib.parse.unquote(parsed.path.lstrip("/"))
        
        # Parse query parameters
        params = urllib.parse.parse_qs(parsed.query)
        
        # Get secret (required)
        secret = params.get("secret", [None])[0]
        if not secret:
            return None
        
        # Get issuer and account name
        issuer = params.get("issuer", [None])[0]
        if ":" in label:
            parts = label.split(":", 1)
            if issuer:
                account_name = parts[1] if len(parts) > 1 else parts[0]
            else:
                issuer = parts[0]
                account_name = parts[1] if len(parts) > 1 else ""
        else:
            account_name = label
            if not issuer:
                issuer = ""
        
        # Build display name
        if issuer and account_name:
            display_name = f"{issuer} ({account_name})"
        elif issuer:
            display_name = issuer
        elif account_name:
            display_name = account_name
        else:
            display_name = "Unknown"
        
        # Get algorithm (default SHA1)
        algorithm = params.get("algorithm", ["SHA1"])[0].upper()
        if algorithm not in ["SHA1", "SHA256", "SHA512", "MD5"]:
            algorithm = "SHA1"
        
        # Get digits (default 6)
        digits = int(params.get("digits", ["6"])[0])
        if digits not in [6, 7, 8]:
            digits = 6
        
        return {
            "name": display_name,
            "secret": secret,
            "digits": digits,
            "algorithm": algorithm
        }
    except Exception as e:
        print(f"   ⚠️  Error parsing URL: {e}")
        return None

def parse_migration_url(url):
    """Parse an otpauth-migration:// URL and extract all accounts"""
    try:
        # Parse the migration URL
        parsed = urllib.parse.urlparse(url)
        if parsed.scheme != "otpauth-migration":
            return None
        
        # Extract and decode the data parameter
        params = urllib.parse.parse_qs(parsed.query)
        if "data" not in params or not params["data"]:
            return None
        
        data = params["data"][0]
        payload = base64.urlsafe_b64decode(data)
        
        # Import the protobuf module
        import auth_migration_pb2
        
        # Parse the migration payload
        migration = auth_migration_pb2.MigrationPayload()
        migration.ParseFromString(payload)
        
        accounts = []
        for otp in migration.otp_parameters:
            # Convert secret bytes to base32 string
            secret_b32 = base64.b32encode(otp.secret).decode("utf-8")
            
            # Build display name from issuer and name
            issuer = otp.issuer if otp.issuer else ""
            name = otp.name if otp.name else ""
            
            if issuer and name:
                display_name = f"{issuer} ({name})"
            elif issuer:
                display_name = issuer
            elif name:
                display_name = name
            else:
                display_name = "Unknown"
            
            # Map algorithm enum to string
            algorithm_map = {
                1: "SHA1",
                2: "SHA256",
                3: "SHA512",
                4: "MD5"
            }
            algorithm = algorithm_map.get(otp.algorithm, "SHA1")
            
            # Get digits (default 6, validate that it's 6, 7, or 8)
            digits = otp.digits if otp.digits in [6, 7, 8] else 6
            
            accounts.append({
                "name": display_name,
                "secret": secret_b32,
                "digits": digits,
                "algorithm": algorithm
            })
        
        return accounts
    except Exception as e:
        print(f"   ⚠️  Error parsing migration URL: {e}")
        return None

def decode_qr_image(img_path):
    """Decode QR code from image and return otpauth URLs"""
    try:
        from pyzbar.pyzbar import decode
        from PIL import Image
        
        img = Image.open(img_path)
        results = decode(img)
        
        urls = []
        for result in results:
            data = result.data.decode("utf-8")
            urls.append(data)
        
        return urls
    except Exception as e:
        print(f"   ❌ Error decoding QR code: {e}")
        return []

def process_qr_code():
    """Automatically process img.png if it exists"""
    if not IMG_PNG.exists():
        return None
    
    print(f"\n📷 Found {IMG_PNG.name}, decoding QR code...")
    urls = decode_qr_image(IMG_PNG)
    
    if not urls:
        print("   ❌ No QR codes found in image!")
        return None
    
    print(f"   ✅ Found {len(urls)} QR code(s)")
    
    accounts = []
    for url in urls:
        if url.startswith("otpauth://"):
            account = parse_otpauth_url(url)
            if account:
                accounts.append(account)
                print(f"   ✅ Parsed: {account['name']}")
        elif url.startswith("otpauth-migration://"):
            print("   🔄 Processing migration URL...")
            migration_accounts = parse_migration_url(url)
            if migration_accounts:
                accounts.extend(migration_accounts)
                print(f"   ✅ Extracted {len(migration_accounts)} account(s) from migration")
            else:
                print("   ❌ Failed to parse migration URL")
        else:
            print(f"   ⚠️  Unknown URL format: {url[:50]}...")
    
    if not accounts:
        print("   ❌ No valid accounts found!")
        return None
    
    return accounts

def create_accounts_json(accounts):
    """Create accounts.json file for C# tray"""
    if not accounts:
        return False
    
    # Create C# format (array)
    csharp_accounts = {
        "accounts": [
            {
                "name": acc["name"],
                "secret": acc["secret"],
                "digits": acc["digits"],
                "algorithm": acc["algorithm"]
            }
            for acc in accounts
        ]
    }
    
    # Check if file exists and choose filename accordingly
    csharp_file = CSHARP_ACCOUNTS_JSON
    
    if CSHARP_ACCOUNTS_JSON.exists():
        csharp_file = CSHARP_TRAY_DIR / "updated_accounts.json"
        print(f"\n⚠️  {CSHARP_ACCOUNTS_JSON.name} already exists, creating {csharp_file.name} instead")
    
    # Write C# accounts.json
    CSHARP_TRAY_DIR.mkdir(parents=True, exist_ok=True)
    with open(csharp_file, "w") as f:
        json.dump(csharp_accounts, f, indent=2)
    print(f"✅ Created {csharp_file}")
    
    return True

def check_accounts_json():
    """Check if accounts.json file exists and is valid"""
    print("\n📋 Checking accounts.json file...")
    print(f"\n  C# tray ({CSHARP_ACCOUNTS_JSON}):")
    if CSHARP_ACCOUNTS_JSON.exists():
        try:
            with open(CSHARP_ACCOUNTS_JSON, "r") as f:
                data = json.load(f)
            print("    ✅ Exists and is valid JSON")
            if isinstance(data, dict) and "accounts" in data:
                count = len(data["accounts"])
            else:
                count = 0
            print(f"    📊 Found {count} account(s)")
            return True
        except json.JSONDecodeError:
            print("    ❌ Invalid JSON!")
            return False
    else:
        if CSHARP_ACCOUNTS_EXAMPLE.exists():
            print("    ⚠️  Not found, but example file exists")
        else:
            print("    ⚠️  Not found")
        return False

def print_next_steps(accounts_exist, qr_processed=False):
    """Print next steps after setup"""
    print("\n" + "=" * 60)
    print("✅ Setup Complete!")
    print("=" * 60)
    
    if qr_processed:
        print("\n📝 QR code processed and accounts.json file created!")
        print("   You can now:")
    else:
        print("\n📝 Next Steps:")
    
    print("\nTo build and run the C# tray application:")
    if accounts_exist:
        print("   ✅ accounts.json exists - ready to build!")
    else:
        print("   ⚠️  accounts.json not found")
    print("   cd csharp-tray/AuthenticatorTray")
    print("   dotnet build")
    print("   dotnet run")
    
    print("\n" + "=" * 60)

def main():
    """Main execution flow"""
    print("=" * 60)
    print("🔐 2FA Authenticator Tray - Setup Script")
    print("=" * 60)
    
    # Step 1: Check Python version
    check_python_version()
    
    # Step 2: Install dependencies
    install_dependencies()
    
    # Step 3: Generate protobuf file
    generate_protobuf()
    
    # Step 4: Check for img.png and process it automatically
    qr_processed = False
    if IMG_PNG.exists():
        accounts = process_qr_code()
        if accounts:
            qr_processed = create_accounts_json(accounts)
    
    # Step 5: Check accounts.json file
    accounts_exist = check_accounts_json()
    
    # Print next steps
    print_next_steps(accounts_exist, qr_processed)

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n👋 Interrupted by user. Goodbye!")
    except Exception as e:
        print(f"\n❌ Error: {e}")
        import traceback
        traceback.print_exc()