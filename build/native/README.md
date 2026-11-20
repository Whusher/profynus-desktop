# Profynus bundled audio runtime

This build uses unmodified, SHA-256-pinned upstream sources. SDL2 and SDL2_mixer are shared libraries under their zlib licenses. FFmpeg is a separate executable with its own static FFmpeg libraries, built without GPL/nonfree options or external codec libraries. See the included LGPL license texts and exact source archives. The executable and libraries remain separate, replaceable files. Keep this entire third-party directory with distributed copies.

Build on the target OS/architecture using Python 3.12+, a C compiler, POSIX shell, make and pkg-config. Linux build hosts also need ALSA/PulseAudio development headers and patchelf. Debian/Ubuntu developer setup: `sudo apt-get install build-essential pkg-config libasound2-dev libpulse-dev patchelf`. Windows builds use MSYS2 MINGW64 (GCC, make, pkg-config and MSYS Python). macOS uses Xcode command-line tools. Python verifies every source archive before extraction. Sources are downloaded only by this developer/CI build script, never by the application.

From the repository root:

```
python3 build/native/build.py --rid linux-x64
```

Supported build targets: linux-x64, linux-arm64, win-x64, osx-x64, osx-arm64. Build each on matching hardware/runner. No cross-compilation is performed. FFmpeg enables only the local audio formats needed by Profynus. All non-WAV inputs are decoded to temporary PCM before reaching the WAV-only mixer, so no codec plugin DLLs are needed. The Vorbis encoder/WebM muxer are included for the synthetic regression fixture.

Linux links only the usual system C/math/thread/dynamic-loader libraries, with SDL loading the OS audio drivers dynamically. ALSA/PulseAudio are operating-system audio services, not bundled codecs. Release Linux builds use Ubuntu 22.04 for x64 and Ubuntu 24.04 for arm64 as their glibc baselines. macOS bundles target macOS 15+. Windows bundles target x64 Windows 10+.

FFmpeg source is from https://ffmpeg.org/; SDL sources are from https://libsdl.org/. See sources.json for exact versions, URLs and checksums. When updating versions, verify the new archives and rerun all native tests on every platform. Native outputs are build artifacts, not committed binaries.

Distributions must include the source archives, notices and this build recipe. Keep the FFmpeg executable separate from Profynus. macOS binaries are ad-hoc signed for local validation; public distribution still requires the publisher's signing and notarization process.

Every configure is followed by a clean compile to prevent stale backend registries. Linux verification loads the resulting SDL binary and requires PulseAudio and ALSA in its compiled driver list; checking only headers or dummy playback is insufficient. OSS and disk-output backends are disabled. PulseAudio (including PipeWire’s PulseAudio compatibility server) routes playback to the desktop’s default output, including Bluetooth headphones.
