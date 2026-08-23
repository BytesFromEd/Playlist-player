using Core.Interfaces;
using Core.Models;
using NAudio.SoundFile;
using NAudio.Wave;

namespace Infrastructure.MediaPlayer;

public class MediaPlayer : IAudioPlayer, IDisposable
{
    private SoundFileReader? currentSong;

    private IAudioOutput player = AudioOutputFactory.Create();

    public long Lenght => currentSong?.Length ?? 0;

    public EventHandler<StoppedEventArgs>? OnStopped
    {
        get => player.OnStopped;
        set => player.OnStopped = value;
    }
    
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

    public void SetSong(Song song)
    {
        if (player.IsPlaying)
            player.Stop();

        currentSong?.Dispose();
        currentSong = new SoundFileReader(song.GetFile() + ".mp3");

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