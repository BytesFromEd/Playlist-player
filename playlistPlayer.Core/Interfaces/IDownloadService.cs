namespace playlistPlayer.Core.Interfaces;

using playlistPlayer.Core.Models;

public interface IDownloadService
{
    public Task<Song[]> DownloadSong(Func<string, string> selector, params string[] ids);
}