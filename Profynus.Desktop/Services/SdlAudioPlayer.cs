using System.Reflection;
using System.Runtime.InteropServices;
namespace Profynus.Desktop.Services;

// SDL_mixer owns decoding and output. The post-mix tap measures actual audible PCM.
public sealed class SdlAudioPlayer : IAudioPlayer
{
    private IntPtr music;
    private bool opened;
    private readonly Native.MixCallback callback;
    private readonly short[] samples = new short[8192];
    private float energy;
    public float Energy => Volatile.Read(ref energy);
    public bool IsPlaying => opened && Native.Mix_PlayingMusic() != 0 && Native.Mix_PausedMusic() == 0;
    public double Position => music == IntPtr.Zero ? 0 : Math.Max(0, Native.Mix_GetMusicPosition(music));
    public double Duration => music == IntPtr.Zero ? 0 : Math.Max(0, Native.Mix_MusicDuration(music));
    public SdlAudioPlayer() { callback = OnAudio; }
    public static IReadOnlyList<string> GetAvailableDrivers()
    {
        Native.EnsureLoaded();
        return Enumerable.Range(0, Native.SDL_GetNumAudioDrivers())
            .Select(index => Marshal.PtrToStringUTF8(Native.SDL_GetAudioDriver(index)) ?? "unknown")
            .ToArray();
    }
    private static string DeviceError() => "Could not open the system's default audio output. " + Error()
        + " Available audio backends: " + string.Join(", ", GetAvailableDrivers()) + ".";

    private void Open()
    {
        if (opened) return;
        try
        {
            Native.EnsureLoaded();
            if (Native.SDL_InitSubSystem(0x10) < 0) throw new InvalidOperationException(DeviceError());

            if (
                Native.Mix_OpenAudio(44100, BitConverter.IsLittleEndian ? (ushort)0x8010 : (ushort)0x9010,
                    2, 4096) < 0) // Expand chunk size from 1024 to 4096 for 93ms of latency support
            {
                var error = DeviceError(); // SDL teardown can overwrite the original device failure.
                Native.SDL_QuitSubSystem(0x10);
                throw new InvalidOperationException(error);
            }
            opened = true;
            Native.Mix_SetPostMix(callback, IntPtr.Zero);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        { throw new InvalidOperationException("The bundled audio runtime could not load. Reinstall the Profynus package for your operating system and architecture.", e); }
    }
    private static string Error() => Marshal.PtrToStringUTF8(Native.SDL_GetError()) ?? "Audio device error";
    public void Play(string path)
    {
        Open();
        var next = Native.Mix_LoadMUS(path);
        if (next == IntPtr.Zero) throw new InvalidOperationException(Error());
        Native.Mix_HaltMusic();
        if (music != IntPtr.Zero) Native.Mix_FreeMusic(music);
        music = next;
        if (Native.Mix_PlayMusic(music, 0) < 0) throw new InvalidOperationException(Error());
    }
    public void TogglePause() { if (!opened) return; if (Native.Mix_PausedMusic() != 0) Native.Mix_ResumeMusic(); else Native.Mix_PauseMusic(); }
    public void Seek(double seconds) { if (music != IntPtr.Zero && Native.Mix_SetMusicPosition(seconds) < 0) throw new InvalidOperationException("Seeking is unavailable for this codec."); }
    public void SetVolume(double volume) { if (opened) Native.Mix_VolumeMusic((int)(Math.Clamp(volume, 0, 100) * 1.28)); }
    private void OnAudio(IntPtr user, IntPtr stream, int length)
    {
        var count = Math.Min(samples.Length, length / 2);
        Marshal.Copy(stream, samples, 0, count);
        double sum = 0;
        for (var i = 0; i < count; i++) { var sample = samples[i] / 32768.0; sum += sample * sample; }
        Volatile.Write(ref energy, count == 0 ? 0 : (float)Math.Min(1, Math.Sqrt(sum / count) * 3));
    }
    public void Dispose()
    {
        if (!opened) return;
        Native.Mix_SetPostMix(null, IntPtr.Zero);
        Native.Mix_HaltMusic();
        if (music != IntPtr.Zero) Native.Mix_FreeMusic(music);
        music = IntPtr.Zero;
        Native.Mix_CloseAudio(); Native.SDL_QuitSubSystem(0x10); opened = false;
    }


    /// <summary>
    ///  Native class for Audio processing loading embedded libraries
    /// </summary>
    private static class Native
    {
        static Native() => NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
        public static void EnsureLoaded() { }
        private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? search)
        {
            if (name is not ("SDL2" or "SDL2_mixer")) return IntPtr.Zero;
            return NativeLibrary.Load(AudioRuntime.LibraryPath(name));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MixCallback(IntPtr user, IntPtr stream, int length);

        // Resampler

        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_GetNumAudioDrivers();
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr SDL_GetAudioDriver(int index);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_InitSubSystem(uint flags);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] public static extern void SDL_QuitSubSystem(uint flags);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr SDL_GetError();

        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Mix_OpenAudio(int frequency, ushort format, int channels, int chunkSize);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern void Mix_CloseAudio();
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern void Mix_SetPostMix(MixCallback? callback, IntPtr user);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Mix_LoadMUS([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern void Mix_FreeMusic(IntPtr music);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern int Mix_PlayMusic(IntPtr music, int loops);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern int Mix_HaltMusic();
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern void Mix_PauseMusic();
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern void Mix_ResumeMusic();
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern int Mix_PlayingMusic();
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern int Mix_PausedMusic();
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern double Mix_GetMusicPosition(IntPtr music);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern double Mix_MusicDuration(IntPtr music);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern int Mix_SetMusicPosition(double position);
        [DllImport("SDL2_mixer", CallingConvention = CallingConvention.Cdecl)] public static extern int Mix_VolumeMusic(int volume);
    }
}
