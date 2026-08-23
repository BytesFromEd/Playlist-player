using NAudio.Wave;

namespace Infrastructure.MediaPlayer;

internal sealed class WindowsAudioOutput : IAudioOutput
{
    private readonly WasapiPlayer player;

    public EventHandler<StoppedEventArgs>? OnStopped { get; set; }
    

    public WindowsAudioOutput()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        player = new WasapiPlayerBuilder().Build();

        player.PlaybackStopped += (sender, args) => { OnStopped?.Invoke(sender, args); };
    }
#pragma warning disable CA1416

    public bool IsPlaying => player.PlaybackState == PlaybackState.Playing;
    
    public void Init(IWaveProvider source)
    {
        player.Init(source);
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
        get => player.Volume;
        set => player.Volume = value;
    }

    public bool Muted
    {
        get => player.IsMuted;
        set => player.IsMuted = value;
    }

    public void Dispose()
    {
        player.Dispose();
    }

#pragma warning restore CA1416
}