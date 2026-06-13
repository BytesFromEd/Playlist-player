namespace playlistPlayer.Core;

interface IAudioPlayer
{
    public void PlaySong(Song song);
    public void Resume();
    public void Pause();
    public void Stop();
    public void Forward();
    public void Backward();
}