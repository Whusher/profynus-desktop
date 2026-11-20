#!/usr/bin/env python3
"""Build a .deb from the published portable Linux release."""
import argparse
import os
import shutil
import subprocess
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ARCH = {"linux-x64": "amd64", "linux-arm64": "arm64"}
GLIBC = {"linux-x64": "2.35", "linux-arm64": "2.39"}   # CI baselines
SIZES = [16, 24, 32, 48, 64, 128, 256, 512]

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--rid", required=True, choices=list(ARCH))
parser.add_argument("--version", default="0.1.0")
parser.add_argument("--icon", default=str(ROOT / "Profynus.Desktop/Assets/profynus.png"))
parser.add_argument("--maintainer", default="Profynus Co.")
parser.add_argument("--homepage", default="profynus.vercel.app")
args = parser.parse_args()

source = ROOT / "artifacts/publish" / args.rid / "Profynus"
if not (source / "Profynus.Desktop").exists():
    raise SystemExit(f"Run publish.py --rid {args.rid} first ({source} not found).")

pkg = ROOT / "artifacts/deb" / args.rid / "profynus"
if pkg.parent.exists(): shutil.rmtree(pkg.parent)
(app := pkg / "opt/profynus").parent.mkdir(parents=True)
shutil.copytree(source, app, symlinks=True)

# Permissions: dirs 755, executables 755, everything else 644.
for dirpath, dirnames, filenames in os.walk(pkg):
    os.chmod(dirpath, 0o755)
    for name in filenames:
        path = Path(dirpath, name)
        if path.is_symlink(): continue
        executable = os.access(path, os.X_OK) or name in ("Profynus.Desktop", "ffmpeg")
        os.chmod(path, 0o755 if executable else 0o644)

def write(path: Path, text: str, mode=0o644):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")
    path.chmod(mode)

# Launcher
write(pkg / "usr/bin/profynus", '#!/bin/sh\nexec /opt/profynus/Profynus.Desktop "$@"\n', 0o755)

# Menu entry
write(pkg / "usr/share/applications/profynus.desktop", """[Desktop Entry]
Type=Application
Version=1.0
Name=Profynus Music
GenericName=Music Player
Comment=Your music. Your atmosphere.
Exec=profynus
Icon=profynus
Terminal=false
Categories=AudioVideo;Audio;Music;Player;
Keywords=music;audio;player;visualizer;
StartupNotify=true
StartupWMClass=profynus
""")

# Icons
logo = Image.open(args.icon).convert("RGBA")
if min(logo.size) < 512: print("Warning: icon is smaller than 512px; use a larger source.")
for size in SIZES:
    target = pkg / f"usr/share/icons/hicolor/{size}x{size}/apps/profynus.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    logo.resize((size, size), Image.LANCZOS).save(target, optimize=True)
    target.chmod(0o644)

# Copyright (replace once you pick an application license)
write(pkg / "usr/share/doc/profynus/copyright", """Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/
Upstream-Name: Profynus

Files: *
Copyright: Profynus authors. All rights reserved.
License: Proprietary

Bundled FFmpeg, SDL2 and SDL2_mixer are under their own licenses (LGPL/zlib).
Their license texts and exact source archives are in /opt/profynus/audio/third-party/.
""")

# Maintainer scripts
refresh = """#!/bin/sh
set -e
if [ "$1" = configure ] || [ "$1" = remove ] || [ "$1" = purge ]; then
    command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database -q /usr/share/applications || true
    command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor || true
fi
exit 0
"""
write(pkg / "DEBIAN/postinst", refresh, 0o755)
write(pkg / "DEBIAN/postrm", refresh, 0o755)

# Control
size_kb = sum(p.stat().st_size for p in pkg.rglob("*") if p.is_file() and "DEBIAN" not in p.parts) // 1024
depends = ", ".join([
    f"libc6 (>= {GLIBC[args.rid]})", "libstdc++6", "zlib1g",
    "libx11-6", "libice6", "libsm6", "libfontconfig1", "libfreetype6",
    "libasound2 | libasound2t64",
])
control = f"""Package: profynus
Version: {args.version}
Architecture: {ARCH[args.rid]}
Maintainer: {args.maintainer}
Installed-Size: {size_kb}
Depends: {depends}
Recommends: libpulse0, pulseaudio | pipewire-pulse
Section: sound
Priority: optional
"""
if args.homepage: control += f"Homepage: {args.homepage}\n"
control += """Description: Profynus Music - local-first music player
 Plays your local music collection with an audio-reactive visualizer,
 24 colour palettes and a floating mini-player. Includes its own audio
 decoder; no files are uploaded.
"""
write(pkg / "DEBIAN/control", control)

out = ROOT / "artifacts/releases" / f"profynus_{args.version}_{ARCH[args.rid]}.deb"
out.parent.mkdir(parents=True, exist_ok=True)
subprocess.run(["dpkg-deb", "--root-owner-group", "-Zxz", "--build", str(pkg), str(out)], check=True)
print("Ready:", out)
