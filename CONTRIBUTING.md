# Contributing

Open `Profynus.sln` with the .NET 10 SDK. `Profynus.Desktop/` and `Profynus.Tests/` are sibling projects; do not open the application project alone. Keep playback and filesystem operations behind service boundaries; views should handle desktop input and presentation. Reuse the shared playback session in additional windows.

Run `dotnet build Profynus.sln` and `dotnet test Profynus.sln --filter 'Category!=NativeAudio'`. Changes to decoding/output also require building the private audio bundle and running the native audio tests described in README.md. Never add a system-codec fallback or a first-launch dependency download. Native source version changes require verified hashes and all target CI jobs to pass. Generate audio fixtures in temporary directories; never commit personal music. Test on the target operating systems before claiming platform support.

Keep secrets, logs, local settings, native binaries and build artifacts out of commits. Use environment variables for machine-specific tool locations. Review staged changes before publishing. Report security issues privately to the repository owner; do not include credentials or private audio in public issues.
