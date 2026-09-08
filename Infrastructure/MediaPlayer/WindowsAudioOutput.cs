using NAudio.Wave;

namespace Infrastructure.MediaPlayer;

internal sealed class WindowsAudioOutput : IAudioOutput
{
    private readonly WasapiPlayer player;
    public event EventHandler<StoppedEventArgs>? OnStopped;
    
    public WindowsAudioOutput()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        player = new WasapiPlayerBuilder().Build();
        
        OnStopped += (sender, args) =>
        {
            Console.WriteLine("EVENT RECEIVED");
        };

        player.PlaybackStopped += (sender, args) =>
        {
            Console.WriteLine("STOPPED");
            OnStopped?.Invoke(sender, args);
        };
    }
#pragma warning disable CA1416

    public bool IsPlaying => player.PlaybackState == PlaybackState.Playing;
    
    public void Init(IWaveProvider source)
    {
        player.Init(source);
        player.Volume = Volume;
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