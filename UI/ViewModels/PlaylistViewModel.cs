using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Infrastructure.Services;
using UI.ViewModels.Models;

namespace UI.ViewModels;

public partial class PlaylistViewModel : ViewModelBase
{
    [ObservableProperty] public partial PlaylistBinding? Playlist { get; private set; }

    [ObservableProperty] public partial ObservableCollection<SongBinding>? Songs { get; set; }

    private readonly Services services = Services.GetInstance();

    public static event EventHandler<MessageArgs>? OnMessage;

    [ObservableProperty] public partial bool IsRefreshing { get; private set; } = false;

    public PlaylistViewModel(PlaylistBinding playlist)
    {
        Tasks =
        [
            .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() =>
            {
                var fullPlaylist = services.GetPlaylist(playlist.Id);
                var playlistBinding = ToBinding(fullPlaylist);
                var songs = playlistBinding.QueueManager.GetSongs().Select(ToBinding).ToList();
                Playlist = playlistBinding;
                Songs = new ObservableCollection<SongBinding>(songs);


                OnNextSong += (_, args) =>
                {
                    switch (args)
                    {
                        case ChangeSongArgs { ChangeIndex: true } change:
                            Tasks =
                            [
                                .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
                                Task.Run(() => { Playlist?.QueueManager.SetIndex(change.SongBinding.Song); })
                            ];
                            break;
                        case NextSongArgs:
                        {
                            var index = Playlist?.QueueManager.Next();
                            if (index == null) return;
                            CurrentSong = Songs?[index.Value];
                            OnNextSong?.Invoke(this, new ChangeSongArgs(CurrentSong!, false));
                            break;
                        }
                        case PrevSongArgs:
                        {
                            var index = Playlist?.QueueManager.Previous();
                            if (index == null) return;
                            CurrentSong = Songs?[index.Value];
                            OnNextSong?.Invoke(this, new ChangeSongArgs(CurrentSong!, false));
                            break;
                        }
                    }
                };

                if (Songs.Count > 0)
                    SongSetter(Songs.First());
            })
        ];
    }

    public PlaylistViewModel()
    {
        var fullPlaylist = services.GetPlaylist(services.GetPlaylists()[0].GetId());
        var playlistBinding = ToBinding(fullPlaylist);
        var songs = playlistBinding.QueueManager.GetSongs().Select(ToBinding).ToList();
        Playlist = playlistBinding;
        Songs = new ObservableCollection<SongBinding>(songs);
        CurrentSong = Songs.First();
        CurrentSong.IsSelected = true;
    }

    [RelayCommand]
    private void SetSong(SongBinding song) => SongSetter(song, true);

    private void SongSetter(SongBinding song, bool changeIndex = false)
    {
        if (song == CurrentSong) return;

        CurrentSong?.IsSelected = false;
        CurrentSong = song;
        CurrentSong?.IsSelected = true;

        Tasks =
        [
            .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() => { Playlist?.QueueManager.SetIndex(song.Song); })
        ];

        OnNextSong?.Invoke(this, new ChangeSongArgs(song, changeIndex));
    }

    [RelayCommand]
    private void Refresh()
    {
        if (Playlist == null || IsRefreshing) return;
        IsRefreshing = true;

        Tasks =
        [
            .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(async () =>
            {
                var temp = await MainViewModel.Services.RefreshPlaylist(Playlist.Playlist, Cts.Token);

                if (temp != Playlist.Playlist && temp != null)
                {
                    Playlist = ToBinding(temp);
                }
            }).ContinueWith(_ =>
            {
                OnMessage?.Invoke(this, new MessageArgs("Refresh done"));
                IsRefreshing = false;
            })
        ];
    }

    [RelayCommand]
    private void Shuffle()
    {
        if (Playlist == null) return;

        Tasks =
        [
            .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() =>
            {
                Playlist.QueueManager.Shuffle();
                Songs = new ObservableCollection<SongBinding>([.. Playlist.QueueManager.GetSongs().Select(ToBinding)]);
                CurrentSong = Songs.First();
                CurrentSong.IsSelected = true;
            })
        ];
    }
}