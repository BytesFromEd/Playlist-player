using Core.Interfaces;
using NAudio.SoundFile;
using NAudio.Wave;

namespace Infrastructure.MediaPlayer;

public class MediaPlayer : IAudioPlayer, IDisposable
{
    private SoundFileReader? currentSong;

    private IAudioOutput player = AudioOutputFactory.Create();

    public MediaPlayer()
    {
        player.OnStopped += (sender, args) => { OnStopped?.Invoke(sender, args); };
    }

    public long Lenght => currentSong?.Length ?? 0;

    public EventHandler<StoppedEventArgs>? OnStopped { get; set; }

    public bool IsPlaying => player.IsPlaying;

    public long Position
    {
        get => currentSong?.Position ?? 0;
        set
        {
            if (currentSong == null) return;

            player.Pause();
            currentSong.Position = value;
            player.Play();
        }
    }

    public string GetProgress()
    {
        if (currentSong == null) return string.Empty;

        var lenght = currentSong.TotalTime;
        var progress = currentSong.CurrentTime;
        return progress.ToString(@"mm\:ss") + " / " + lenght.ToString(@"mm\:ss");
    }

    public float Volume
    {
        get => player.Volume;
        set => player.Volume = (float)((Math.Exp(value) - 1) / (Math.E - 1));
    }

    public bool Muted
    {
        get => player.Muted;
        set => player.Muted = value;
    }

    public void SetSong(string path)
    {
        if (player.IsPlaying)
            player.Stop();

        currentSong?.Dispose();
        currentSong = new SoundFileReader(path);

        var oldVolume = player.Volume;
        var muted = player.Muted;

        player.Dispose();
        player = AudioOutputFactory.Create();
        player.Init(currentSong);

        player.Volume = oldVolume;
        player.Muted = muted;
    }

    public void PlayStop()
    {
        if (player.IsPlaying)
            player.Pause();
        else
            player.Play();
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

    public void Dispose()
    {
        currentSong?.Dispose();
        player.Dispose();
    }
}