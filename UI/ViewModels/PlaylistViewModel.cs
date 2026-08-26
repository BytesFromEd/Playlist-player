using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Services;
using UI.ViewModels.Models;

namespace UI.ViewModels;

public partial class PlaylistViewModel : ViewModelBase
{
    public event EventHandler<MessageArgs>? OnMessage;

    [ObservableProperty] public partial bool IsRefreshing { get; private set; } = false;

    [ObservableProperty] public partial State State { get; set; }

    public PlaylistViewModel(State state)
    {
        State = state;
    }


    [RelayCommand]
    private void SetSong(SongBinding song)
    {
        if (song == State.CurrentSong) return;

        State.CurrentSong = song;
        State.Tasks =
        [
            .. State.Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() => { State.CurrentPlaylist?.QueueManager.SetIndex(song.Song); })
        ];
    }

    [RelayCommand]
    private void Refresh()
    {
        if (State.CurrentPlaylist == null || IsRefreshing) return;
        IsRefreshing = true;
        State.Tasks =
        [
            .. State.Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(async () =>
            {
                var temp = await State.Services.RefreshPlaylist(State.CurrentPlaylist.Playlist, State.Cts.Token);

                if (temp != State.CurrentPlaylist.Playlist && temp != null)
                {
                    State.CurrentPlaylist = State.ToBinding(temp);
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
        if (State.CurrentPlaylist == null) return;
        State.Tasks =
        [
            .. State.Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() =>
            {
                State.CurrentPlaylist.QueueManager.Shuffle();
                State.Songs = new ObservableCollection<SongBinding>([
                    .. State.CurrentPlaylist.QueueManager.GetSongs().Select(State.ToBinding)
                ]);
                State.CurrentSong = State.Songs.First();
            })
        ];
    }
}