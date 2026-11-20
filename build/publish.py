#!/usr/bin/env python3
"""Publish a self-contained portable release with verified offline audio dependencies."""
import argparse
from pathlib import Path
import plistlib
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "build/native"))
from verify import verify

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--rid", required=True, choices=["linux-x64", "linux-arm64", "win-x64", "osx-x64", "osx-arm64"])
args = parser.parse_args()
bundle = ROOT / "artifacts/audio" / args.rid
verify(bundle)
staging = ROOT / "artifacts/publish" / args.rid
if staging.exists(): shutil.rmtree(staging)
package = staging / ("Profynus.app" if args.rid.startswith("osx") else "Profynus")
output = package / "Contents/MacOS" if args.rid.startswith("osx") else package
subprocess.run(["dotnet", "publish", str(ROOT / "Profynus.Desktop/Profynus.Desktop.csproj"),
    "--configuration", "Release", "--runtime", args.rid, "--self-contained", "true", "-o", str(output)], check=True)
verify(output / "audio")
if not args.rid.startswith("win"):
    (output / "audio/ffmpeg").chmod(0o755)
    (output / "Profynus.Desktop").chmod(0o755)
if args.rid.startswith("osx"):
    info = {"CFBundleName": "Profynus", "CFBundleDisplayName": "Profynus", "CFBundleIdentifier": "com.profynus.desktop",
        "CFBundleExecutable": "Profynus.Desktop", "CFBundlePackageType": "APPL", "CFBundleShortVersionString": "0.1.0",
        "CFBundleVersion": "1", "LSMinimumSystemVersion": "15.0", "NSHighResolutionCapable": True,
        "LSApplicationCategoryType": "public.app-category.music"}
    with (package / "Contents/Info.plist").open("wb") as file: plistlib.dump(info, file)
    subprocess.run(["codesign", "--force", "--sign", "-", str(package)], check=True)
release = ROOT / "artifacts/releases"
release.mkdir(parents=True, exist_ok=True)
archive = shutil.make_archive(str(release / ("Profynus-" + args.rid)), "zip" if args.rid.startswith("win") else "gztar", staging, package.name)
print("Ready:", archive)
