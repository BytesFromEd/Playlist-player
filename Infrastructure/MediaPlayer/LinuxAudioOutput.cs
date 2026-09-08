using NAudio.Wave;
using NAudio.Wave.Alsa;

namespace Infrastructure.MediaPlayer;

internal sealed class LinuxAudioOutput : IAudioOutput
{
    private readonly AlsaOut player;
    private float? volume;
    public event EventHandler<StoppedEventArgs>? OnStopped;
    public LinuxAudioOutput()
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException();
        }

        player = new AlsaOut();
        

        player.PlaybackStopped += (sender, args) => { OnStopped?.Invoke(sender, args); };
    }
#pragma warning disable CA1416

    public bool IsPlaying => player.PlaybackState == PlaybackState.Playing;

    public void Init(IWaveProvider source)
    {
        player.Init(source);
        player.Volume = volume ?? 1;
    }

    public void Play()
    {
        player.Play();
    }

    public void Pause()
    {
        player.Pause();
    }

    public void Stop()
    {
        player.Stop();
    }

    public float Volume
    {
        get => Muted ? volume ?? 1 : player.Volume;
        set
        {
            if (Muted)
            {
                Muted = false;
            }

            player.Volume = value;
        }
    }

    public bool Muted
    {
        get => player.Volume == 0;
        set
        {
            if (value)
            {
                player.Volume = volume ?? 1;
            }
            else
            {
                volume = player.Volume;
                player.Volume = 0;
            }
        }
    }

    public void Dispose()
    {
        player.Dispose();
    }

#pragma warning restore CA1416
}