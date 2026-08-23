namespace Core.Interfaces;

using Models;

public interface IAudioPlayer
{
    public void SetSong(Song song);
    public void Play();
    public void Pause();
    public void Stop();
    public void PlayStop();
}