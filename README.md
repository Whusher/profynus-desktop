# Profynus Desktop

A local-first music player built with .NET 10 and Avalonia: 24 palettes, an audio-reactive globe, a transparent floating mini-player, and private local playback. Accounts and backend integration come later.

## Using a release

Extract the package for your OS/architecture and keep its contents together. Launch `Profynus.Desktop.exe` on Windows, `Profynus.Desktop` on Linux, or `Profynus.app` on macOS. **Release packages include .NET, FFmpeg, SDL2 and SDL2_mixer. Users do not install audio codecs or download anything on first launch.** Normal OS desktop/audio services are still required.

| Target | Package | Build baseline |
| --- | --- | --- |
| Windows x64 | ZIP | Windows 10+ |
| Linux x64 | tar.gz | Ubuntu 22.04 / glibc 2.35+ |
| Linux ARM64 | tar.gz | Ubuntu 24.04 / glibc 2.39+ |
| macOS Intel | `.app` in tar.gz | macOS 15+ |
| macOS Apple Silicon | `.app` in tar.gz | macOS 15+ |

The CI workflow builds on each target platform, checks native dependency closure, tests bundled playback and uploads portable packages. Windows/macOS signing and macOS notarization require publisher credentials and are not configured. Local Linux builds inherit the host's glibc baseline; use CI's baseline runners for distributable builds. Windows ARM64 and Linux musl are not supported by this packaging matrix.

## Solution and development

Open **`Profynus.sln`**, not an individual `.csproj`, in Rider. Both projects are top-level solution entries:

```text
Profynus.sln
Profynus.Desktop/       Avalonia application
  Models/
  Services/
  ViewModels/
  Controls/
  Assets/
Profynus.Tests/         xUnit tests, referencing the application
build/                 Native dependency build, verification and packaging
Directory.Build.targets
```

If Rider still shows only the desktop project, close that project and open `Profynus.sln`. There is no nested tests folder or old smoke console project.

Install .NET 10 and the developer build tools listed in [build/native/README.md](build/native/README.md). These are build-machine requirements, not end-user requirements. Build the native bundle once for your host, then launch:

```sh
python3 build/native/build.py --rid linux-x64
python3 build/native/verify.py artifacts/audio/linux-x64
dotnet build Profynus.sln
dotnet run --project Profynus.Desktop/Profynus.Desktop.csproj
```

Use the appropriate target RID on other systems. Windows native builds run in MSYS2 MINGW64 with MSYS Python. Ordinary builds can run without the native bundle for unit testing; playback requires it. MSBuild copies the matching bundle to application and test output directories. Publishing fails when the bundle is missing or the application is not self-contained.

## Packaging

```sh
python3 build/publish.py --rid linux-x64
```

This verifies the bundle, publishes a self-contained app, verifies the copied binaries again, and creates an archive in `artifacts/releases/`. The same script packages Windows and macOS on their respective hosts. Packages include upstream license notices, SHA-256-pinned source archives and the build recipe in `audio/third-party/`. Keep those files with distributions. Do not commit generated binaries or native source archives.

## Audio architecture

The app loads **only its own `audio/` directory**. There is no PATH lookup, environment override, system SDL fallback or runtime download. The native build uses pinned upstream sources and rejects checksum mismatches. FFmpeg is a separate, replaceable executable with built-in audio codecs; SDL_mixer is built for WAV only, avoiding hidden external codec dependencies. Linux SDL uses dynamically loaded OS audio drivers; macOS/Windows use their platform audio services.

Discovery recognizes MP3, FLAC, WAV, OGG, OPUS, AIFF, WebM, M4A and AAC. WAV goes directly to the mixer; other formats are decoded to temporary stereo PCM using bundled FFmpeg. FFmpeg probes content, so a WebM file incorrectly named `.mp3` works without renaming or modifying the original. Switching tracks cancels pending preparation; track replacement or application exit removes temporary audio. Preparation has a three-minute timeout and needs disk space for decoded PCM. Long tracks can take time to start. Streaming decoding is a future improvement.

The application scans the OS Music folder, `~/Music`, Linux's XDG Music directory and folders added by the user. Search filters titles, artists and formats. Double-click or press Enter to play. Playback snapshots the filtered collection as a queue. Previous/next wrap; automatic advancement stops at the end. Metadata currently comes from filenames; embedded tags and artwork are future work.

`IAudioPlayer` separates audio output from presentation. `AudioFilePreparation` handles decoding; `AudioRuntime` resolves private binaries. Models, discovery, settings and playback state live outside the window code. Future backend services should leave local listening independent of sign-in.

## Visualizer and mini-player

The globe is perspective-projected geometry driven by RMS energy from actual audible PCM. Four profiles adjust deformation intensity; reduced motion freezes rotation and deformation. BPM detection and frequency-band analysis are not implemented yet.

Click **Mini player** to open the draggable, always-on-top overlay and minimize the library. It shares the same audio session, queue and palette. Controls disappear three seconds after the pointer leaves; the globe and song title remain. Hover reveals controls. Keyboard interaction keeps controls available while active; Escape closes the overlay. Closing the mini-player restores the library. Closing the main application stops playback and closes both windows.

Transparency needs an OS compositor; always-on-top and positioning depend on window-manager policy. The overlay is not click-through. Preferences persist in the OS local application-data directory under `Profynus/settings.json`. No audio is uploaded.

## Tests

```sh
dotnet test Profynus.sln --filter 'Category!=NativeAudio'
SDL_AUDIODRIVER=dummy dotnet test Profynus.sln
```

The first command needs no native bundle. Build the bundle before running all tests. In Rider, run `Profynus.Tests` with `SDL_AUDIODRIVER=dummy` in the test environment to avoid audible output. SDL tests run serially because SDL_mixer owns process-global state.

Tests cover discovery, cancellation, file preparation, private runtime resolution, mini-player control visibility, PCM output, pause/resume, seeking, volume, completion and generated WebM audio deliberately named `.mp3`. Fixtures are generated temporarily; no personal audio is committed. Native tests use bundled FFmpeg/SDL rather than system-installed binaries.

Linux bundle verification and local tests have been run. Other target packages require successful CI runner results and physical-device smoke tests before release; workflow configuration alone is not evidence of platform validation.

## Repository hygiene

Generated output, credentials, IDE state, agent directories and local music are ignored. No remote is configured. Review staged files before publishing. No application license has been selected yet; dependency license texts are supplied with packages. See [CONTRIBUTING.md](CONTRIBUTING.md).
