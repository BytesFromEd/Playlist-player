namespace Infrastructure.MediaPlayer;

internal static class AudioOutputFactory
{
    public static IAudioOutput Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsAudioOutput();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxAudioOutput();
        }

        throw new NotSupportedException("This platform does not support audio outputs");
    }
}