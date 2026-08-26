using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Infrastructure;
using UI.ViewModels.Models;

namespace UI.Services;

public partial class State : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<PlaylistBinding>? Playlists { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongBinding>? Songs { get; set; }
    [ObservableProperty] public partial SongBinding? CurrentSong { get; set; }
    [ObservableProperty] public partial PlaylistBinding? CurrentPlaylist { get; set; }

    public List<Task> Tasks = [];
    public readonly AppSettings AppSettings = new();
    public readonly Infrastructure.Services.Services Services = new();
    public readonly CancellationTokenSource Cts = new();

    public event EventHandler? OnSongChanged;

    partial void OnCurrentPlaylistChanged(PlaylistBinding? oldValue, PlaylistBinding? newValue)
    {
        if (newValue?.Id == null || newValue.Id == oldValue?.Id)
            return;

        Tasks =
        [
            .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() =>
            {
                var fullPlaylist = Services.GetPlaylist(newValue.Id);
                var playlistBinding = ToBinding(fullPlaylist);
                var songs = playlistBinding.QueueManager.GetSongs().Select(ToBinding).ToList();
                CurrentPlaylist = playlistBinding;
                Songs = new ObservableCollection<SongBinding>(songs);

                if (Songs.Count <= 0) return;

                CurrentSong?.IsSelected = false;
                CurrentSong = Songs.First();
                CurrentSong.IsSelected = true;
            })
        ];
    }

    partial void OnCurrentSongChanged(SongBinding? oldValue, SongBinding? newValue)
    {
        oldValue?.IsSelected = false;
        newValue?.IsSelected = true;
        if (oldValue?.Song.GetId() == newValue?.Song.GetId())
        {
            return;
        }

        OnSongChanged?.Invoke(this, EventArgs.Empty);
    }

    internal PlaylistBinding ToBinding(Playlist playlist)
    {
        return playlist.GetThumbanil() == null
            ? new PlaylistBinding(playlist, null)
            : new PlaylistBinding(playlist, new Bitmap(Path.Combine(AppSettings.AppFolder, playlist.GetThumbanil()!)));
    }

    internal SongBinding ToBinding(Song song)
    {
        var image = new Bitmap(Path.Combine(AppSettings.AppFolder, "songs", song.GetCover()));

        var sec = song.GetDuration() % 60;
        var min = (song.GetDuration() - sec) / 60;

        var duration = $"{min:00}:{sec:00}";

        return new SongBinding(song, image, duration);
    }
}