namespace playlistPlayer.Core.Interfaces;

using playlistPlayer.Core.Models;

public interface IDownloadService
{
    public Task<Song[]> DownloadSong(params Song[] songs);
}