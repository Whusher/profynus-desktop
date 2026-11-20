#!/usr/bin/env python3
"""Verify bundle hashes and reject accidentally linked build-host codec/runtime dependencies."""
import argparse
import ctypes
import hashlib
import json
from pathlib import Path
import re
import subprocess


def verify(directory):
    manifest = json.loads((directory / "runtime.json").read_text())
    rid = manifest["rid"]
    for name, digest in manifest["files"].items():
        path = directory / name
        if hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            raise RuntimeError(f"Bundle hash mismatch: {name}")
        if rid.startswith("linux"):
            output = subprocess.check_output(["ldd", str(path)], text=True)
            allowed = ("linux-vdso", "ld-linux", "libc.so", "libm.so", "libpthread.so", "libdl.so", "librt.so", "libSDL2-")
            for line in output.splitlines():
                library = line.strip().split()[0]
                if "not found" in line or not any(token in library for token in allowed):
                    raise RuntimeError(f"Unbundled Linux dependency: {line.strip()}")
                if "libSDL2-" in library and str(directory.resolve()) not in line:
                    raise RuntimeError("Mixer resolves SDL outside the bundle")
        elif rid.startswith("osx"):
            output = subprocess.check_output(["otool", "-L", str(path)], text=True)
            for line in output.splitlines()[1:]:
                dependency = line.strip().split(" (", 1)[0]
                if not dependency.startswith(("/usr/lib/", "/System/Library/", "@loader_path/")):
                    raise RuntimeError(f"Non-relocatable macOS dependency: {dependency}")
        elif rid.startswith("win"):
            output = subprocess.check_output(["objdump", "-p", str(path)], text=True)
            allowed = {"sdl2.dll", "kernel32.dll", "user32.dll", "advapi32.dll", "msvcrt.dll", "ucrtbase.dll",
                "winmm.dll", "ole32.dll", "oleaut32.dll", "shell32.dll", "setupapi.dll", "version.dll",
                "ws2_32.dll", "gdi32.dll", "imm32.dll", "ntdll.dll", "bcrypt.dll", "secur32.dll", "rpcrt4.dll",
                "shlwapi.dll", "cfgmgr32.dll", "dinput8.dll", "dxguid.dll"}
            for dependency in re.findall(r"DLL Name:\s*(\S+)", output):
                if dependency.lower() not in allowed and not dependency.lower().startswith("api-ms-win-"):
                    raise RuntimeError(f"Unbundled Windows dependency: {dependency}")
    # A dummy-output smoke test cannot prove that a real device backend was compiled.
    if rid.startswith("linux"):
        sdl = ctypes.CDLL(str(directory / "libSDL2-2.0.so.0"))
        sdl.SDL_GetAudioDriver.restype = ctypes.c_char_p
        drivers = {sdl.SDL_GetAudioDriver(i).decode() for i in range(sdl.SDL_GetNumAudioDrivers())}
        missing = {"pulseaudio", "alsa"} - drivers
        if missing:
            raise RuntimeError(f"Bundled SDL is missing real audio backends: {sorted(missing)}; compiled drivers: {sorted(drivers)}")
        print("Compiled Linux audio drivers:", ", ".join(sorted(drivers)))
    legal = directory / "third-party"
    for spec in manifest["sources"].values():
        archive = legal / spec["url"].split("/")[-1]
        if hashlib.sha256(archive.read_bytes()).hexdigest() != spec["sha256"]:
            raise RuntimeError("Missing or modified corresponding source archive")
    print("Verified relocatable audio bundle:", rid)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    verify(parser.parse_args().directory.resolve())
