namespace Core.Interfaces;

using Models;

public interface IDownloadService
{
    public Task DownloadSongs(string playlist, CancellationToken ct, params List<Song> songs);
}