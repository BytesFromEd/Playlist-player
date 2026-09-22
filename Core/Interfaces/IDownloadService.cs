namespace Core.Interfaces;

using Models;

public interface IDownloadService
{
    public Task DownloadSongs(string playlistId, CancellationToken ct, params List<Song> songs);
}