namespace playlistPlayer.Core;

interface IDownloadService
{
    public Song DownloadSong(string id);
}