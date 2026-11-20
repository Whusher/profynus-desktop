#!/usr/bin/env python3
"""Build private, relocatable audio dependencies. Run natively; Windows uses MSYS2 MINGW64 Python."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import tarfile
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
SUPPORTED = {"linux-x64", "linux-arm64", "win-x64", "osx-x64", "osx-arm64"}

def run(args, cwd, env):
    print("+", " ".join(map(str, args)), flush=True)
    subprocess.run(list(map(str, args)), cwd=cwd, env=env, check=True)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rid", required=True, choices=sorted(SUPPORTED))
    parser.add_argument("--jobs", type=int, default=min(os.cpu_count() or 2, 8))
    args = parser.parse_args()
    system = platform.system().lower()
    host_os = "win" if "mingw" in system or "msys" in system or system == "windows" else "osx" if system == "darwin" else "linux"
    host_arch = "arm64" if platform.machine().lower() in ("aarch64", "arm64") else "x64"
    if args.rid != f"{host_os}-{host_arch}":
        parser.error("Build on the target OS/architecture; cross-compilation is not configured.")
    work = ROOT / "artifacts/native" / args.rid
    cache = ROOT / "artifacts/native/downloads"
    prefix = work / "install"
    output = ROOT / "artifacts/audio" / args.rid
    work.mkdir(parents=True, exist_ok=True); cache.mkdir(parents=True, exist_ok=True)
    (output / "runtime.json").unlink(missing_ok=True)
    env = os.environ.copy()
    env["PATH"] = str(prefix / "bin") + os.pathsep + env["PATH"]
    env["PKG_CONFIG_PATH"] = str(prefix / "lib/pkgconfig") + os.pathsep + env.get("PKG_CONFIG_PATH", "")
    if host_os == "win":
        env["CFLAGS"] = "-O2"
        env["LDFLAGS"] = "-static-libgcc"
    if host_os == "osx":
        env["MACOSX_DEPLOYMENT_TARGET"] = "15.0"
    sources = json.loads((ROOT / "build/native/sources.json").read_text())
    extracted = {}
    for name, spec in sources.items():
        archive = cache / spec["url"].split("/")[-1]
        if not archive.exists():
            temp = archive.with_suffix(archive.suffix + ".download")
            urllib.request.urlretrieve(spec["url"], temp)
            temp.replace(archive)
        if hashlib.sha256(archive.read_bytes()).hexdigest() != spec["sha256"]:
            raise RuntimeError(f"Source checksum mismatch: {archive}; delete it before retrying")
        with tarfile.open(archive) as tar:
            directory = work / tar.getnames()[0].split("/")[0]
            if not directory.exists(): tar.extractall(work, filter="data")
        extracted[name] = directory

    def compile_source(name, flags):
        source = extracted[name]
        configure = ["sh", "./configure", "--prefix=" + str(prefix), *flags]
        run(configure, source, env)
        # configure changes are not reliably tracked by upstream generated makefiles.
        # Never reuse objects built without the audio driver headers.
        run(["make", "clean"], source, env)
        run(["make", "-j" + str(args.jobs)], source, env)
        run(["make", "install"], source, env)

    compile_source("sdl", ["--disable-static", "--enable-shared", "--disable-video", "--disable-render",
        "--disable-joystick", "--disable-haptic", "--disable-sensor", "--disable-power",
        "--disable-jack", "--disable-pipewire", "--disable-esd", "--disable-arts", "--disable-nas",
        "--disable-sndio", "--disable-oss", "--disable-diskaudio", "--disable-libsamplerate"])
    if host_os == "linux":
        config = (extracted["sdl"] / "include/SDL_config.h").read_text()
        if "#define SDL_AUDIO_DRIVER_ALSA 1" not in config or "#define SDL_AUDIO_DRIVER_PULSEAUDIO 1" not in config:
            raise RuntimeError("Linux builds require ALSA and PulseAudio development headers; refusing to ship a dummy-only audio backend.")
    compile_source("mixer", ["--disable-static", "--enable-shared", "--disable-sdltest",
        "--with-sdl-prefix=" + str(prefix), "--disable-music-cmd", "--enable-music-wave",
        *["--disable-music-" + codec for codec in ("mod", "midi", "gme", "ogg", "flac", "mp3", "opus", "wavpack")]])
    compile_source("ffmpeg", ["--disable-autodetect", "--disable-everything", "--disable-network",
        "--disable-doc", "--disable-debug", "--disable-x86asm", "--disable-shared", "--enable-static",
        "--disable-ffplay", "--disable-ffprobe", "--enable-ffmpeg",
        "--enable-protocol=file,pipe", "--enable-filter=aresample,anull",
        "--enable-demuxer=wav,mp3,flac,ogg,matroska,mov,aac,aiff",
        "--enable-parser=aac,aac_latm,flac,mpegaudio,opus,vorbis",
        "--enable-decoder=mp3,mp3float,mp3adu,mp3adufloat,flac,vorbis,opus,aac,aac_fixed,alac,pcm_s16le,pcm_s16be,pcm_s24le,pcm_s24be,pcm_s32le,pcm_s32be,pcm_f32le,pcm_f32be,pcm_f64le,pcm_f64be,pcm_u8,pcm_s8,pcm_alaw,pcm_mulaw",
        "--enable-encoder=pcm_s16le,vorbis", "--enable-muxer=wav,webm",
        *(["--extra-ldflags=-static", "--target-os=mingw32"] if host_os == "win" else [])])
    if output.exists(): shutil.rmtree(output)
    output.mkdir(parents=True)
    if host_os == "win":
        files = {"SDL2.dll": prefix / "bin/SDL2.dll", "SDL2_mixer.dll": prefix / "bin/SDL2_mixer.dll", "ffmpeg.exe": prefix / "bin/ffmpeg.exe"}
    elif host_os == "osx":
        files = {"libSDL2.dylib": prefix / "lib/libSDL2.dylib", "libSDL2_mixer.dylib": prefix / "lib/libSDL2_mixer.dylib", "ffmpeg": prefix / "bin/ffmpeg"}
    else:
        files = {"libSDL2-2.0.so.0": prefix / "lib/libSDL2-2.0.so.0", "libSDL2_mixer-2.0.so.0": prefix / "lib/libSDL2_mixer-2.0.so.0", "ffmpeg": prefix / "bin/ffmpeg"}
    for name, path in files.items(): shutil.copy2(path, output / name)
    if host_os == "linux":
        run(["patchelf", "--set-rpath", "$ORIGIN", output / "libSDL2_mixer-2.0.so.0"], work, env)
        run(["patchelf", "--remove-rpath", output / "libSDL2-2.0.so.0"], work, env)
    if host_os == "osx":
        for lib in ("libSDL2.dylib", "libSDL2_mixer.dylib"):
            run(["install_name_tool", "-id", "@loader_path/" + lib, output / lib], work, env)
        links = subprocess.check_output(["otool", "-L", str(output / "libSDL2_mixer.dylib")], text=True)
        for line in links.splitlines()[1:]:
            dependency = line.strip().split(" (", 1)[0]
            if "SDL2-" in dependency or dependency.endswith("/libSDL2.dylib"):
                run(["install_name_tool", "-change", dependency, "@loader_path/libSDL2.dylib", output / "libSDL2_mixer.dylib"], work, env)
        for name in files: run(["codesign", "--force", "--sign", "-", output / name], work, env)
    # Ship exact corresponding sources and the recipe with every runtime.
    legal = output / "third-party"
    legal.mkdir()
    for name, spec in sources.items():
        shutil.copy2(cache / spec["url"].split("/")[-1], legal)
        for license_file in extracted[name].glob("*COPYING*"):
            if license_file.is_file(): shutil.copy2(license_file, legal / (name + "-" + license_file.name))
        for license_file in extracted[name].glob("LICENSE*"):
            if license_file.is_file(): shutil.copy2(license_file, legal / (name + "-" + license_file.name))
    for name in ("sources.json", "build.py", "README.md"):
        shutil.copy2(ROOT / "build/native" / name, legal / name)
    manifest = {"rid": args.rid, "sources": sources, "files": {name: hashlib.sha256((output / name).read_bytes()).hexdigest() for name in files}}
    (output / "runtime.json").write_text(json.dumps(manifest, indent=2) + "\n")
    from verify import verify
    verify(output.resolve())
    print("Audio bundle ready:", output)

if __name__ == "__main__": main()
