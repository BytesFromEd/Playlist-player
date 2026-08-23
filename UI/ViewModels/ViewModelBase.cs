using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using UI.ViewModels.Models;

namespace UI.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<PlaylistBinding> Playlists { get; set; } = [];

    public static List<Task> Tasks = [];
    public static readonly CancellationTokenSource Cts = new();

    [ObservableProperty] public partial SongBinding? CurrentSong { get; protected set; }

    protected static EventHandler<SongEventArgs>? OnNextSong;

    internal static PlaylistBinding ToBinding(Playlist playlist)
    {
        return playlist.GetThumbanil() == null
            ? new PlaylistBinding(playlist, null)
            : new PlaylistBinding(playlist, new Bitmap(playlist.GetThumbanil()!));
    }

    internal static SongBinding ToBinding(Song song)
    {
        var image = new Bitmap(song.GetFile() + ".webp");

        var sec = song.GetDuration() % 60;
        var min = (song.GetDuration() - sec) / 60;

        var duration = $"{min:00}:{sec:00}";

        return new SongBinding(song, image, duration);
    }
}