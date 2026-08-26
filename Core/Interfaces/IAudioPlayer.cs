namespace Core.Interfaces;

public interface IAudioPlayer
{
    public void SetSong(string path);
    public void Play();
    public void Pause();
    public void Stop();
    public void PlayStop();
}