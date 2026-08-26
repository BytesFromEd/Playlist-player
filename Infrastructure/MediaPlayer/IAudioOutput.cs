using NAudio.Wave;

namespace Infrastructure.MediaPlayer;

internal interface IAudioOutput : IDisposable
{
    event EventHandler<StoppedEventArgs>? OnStopped;
    void Init(IWaveProvider source);
    void Play();
    void Pause();
    void Stop();
    float Volume { get; set; }
    bool Muted { get; set; }
    public bool IsPlaying { get; }
}