using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Infrastructure;
using Infrastructure.MediaPlayer;
using UI.Services;
using UI.ViewModels.Models;


namespace UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial ViewModelBase? CurrentViewModel { get; set; }
    [ObservableProperty] public partial string Url { get; set; } = string.Empty;
    [ObservableProperty] public partial string Message { get; set; } = string.Empty;
    [ObservableProperty] public partial float Volume { get; set; }
    [ObservableProperty] public partial long Progress { get; set; } = 0;
    [ObservableProperty] public partial long Lenght { get; set; } = 0;
    [ObservableProperty] public partial string Time { get; set; } = "";

    [ObservableProperty] public partial Geometry? VolumeIcon { get; set; }
    [ObservableProperty] public partial Geometry? PlayIcon { get; set; }

    private readonly SettingsViewModel settingsViewModel;
    private readonly PlaylistViewModel playlistViewModel;

    public bool IsSeeking
    {
        get;
        set
        {
            if (value)
            {
                positionTimer.Stop();
                player?.Pause();
            }

            field = value;
        }
    }

    private readonly MediaPlayer? player;
    private bool isMuted;

    private readonly DispatcherTimer positionTimer;

    public event EventHandler<long>? PositionChanged;

    [ObservableProperty] public partial State State { get; set; }

    public MainViewModel(State state, PlaylistViewModel playlistViewModel, SettingsViewModel settingsViewModel)
    {
        Volume = state.AppSettings.Volume;
        State = state;

        this.playlistViewModel = playlistViewModel;
        this.settingsViewModel = settingsViewModel;

        state.Services.CreateTable();

        var playlists = state.Services.GetPlaylists().Select(state.ToBinding).ToList();
        if (playlists.Count > 0)
        {
            CurrentViewModel = playlistViewModel;
            State.CurrentPlaylist = playlists.First();
            State.CurrentPlaylist.IsSelected = true;
        }
        else
        {
            CurrentViewModel = settingsViewModel;
        }

        state.Playlists = new ObservableCollection<PlaylistBinding>([.. playlists]);


        player = new MediaPlayer();

        positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        positionTimer.Tick += (_, _) =>
        {
            if (Progress == Lenght)
            {
                Next();
            }
            else if (player is { IsPlaying: true })
                PositionChanged?.Invoke(this, player.Position);
        };

        PositionChanged += (_, args) => { Progress = args; };

        state.OnSongChanged += (_, _) =>
        {
            var lastStatePlay = player.IsPlaying;

            positionTimer.Stop();

            if (State.CurrentSong == null)
                return;

            player.SetSong(Path.Combine(State.AppSettings.AppFolder, "songs", State.CurrentSong.Song.GetFile()));
            Lenght = player.Lenght;
            IsSeeking = false;
            Progress = 0;

            if (!lastStatePlay) return;

            player.Play();
            positionTimer.Start();
        };

        OnVolumeChanged(Volume);

        if (Application.Current!.TryGetResource("PlayIcon", Application.Current.ActualThemeVariant, out var res))
            PlayIcon = res as Geometry;

        playlistViewModel.OnMessage += (_, args) => { Message = args.Message; };
        state.Services.OnServiceMessage += (_, args) => { Message = args.Message; };
        //player.OnStopped += (_, args) => { Console.WriteLine("ON MAIN"); }; //to fix
    }

    partial void OnProgressChanged(long value)
    {
        Time = player?.GetProgress() ?? string.Empty;
    }

    partial void OnVolumeChanged(float value)
    {
        isMuted = false;

        if (value < 0.25)
        {
            if (Application.Current!.TryGetResource("VolumeNoneIcon", Application.Current.ActualThemeVariant,
                    out var res))
                VolumeIcon = res as Geometry;
        }
        else if (value < 0.5)
        {
            if (Application.Current!.TryGetResource("VolumeLowIcon", Application.Current.ActualThemeVariant,
                    out var res))
                VolumeIcon = res as Geometry;
        }
        else
        {
            if (Application.Current!.TryGetResource("VolumeFullIcon", Application.Current.ActualThemeVariant,
                    out var res))
                VolumeIcon = res as Geometry;
        }

        player?.Volume = value;
    }

    public void EndSeek()
    {
        IsSeeking = false;
        if (player is null) return;
        if (Progress > Lenght * 0.95)
        {
            Next();
            return;
        }

        player.Position = Progress;
        player.Play();
        positionTimer.Start();

        if (Application.Current!.TryGetResource("PauseIcon", Application.Current.ActualThemeVariant,
                out var res))
            PlayIcon = res as Geometry;
    }

    [RelayCommand]
    private void Play()
    {
        player?.PlayStop();


        if (player?.IsPlaying ?? false)
        {
            positionTimer.Start();
            if (Application.Current!.TryGetResource("PauseIcon", Application.Current.ActualThemeVariant,
                    out var res))
                PlayIcon = res as Geometry;
        }
        else
        {
            positionTimer.Stop();
            if (Application.Current!.TryGetResource("PlayIcon", Application.Current.ActualThemeVariant,
                    out var res))
                PlayIcon = res as Geometry;
        }
    }

    [RelayCommand]
    private void Prev()
    {
        var index = State.CurrentPlaylist?.QueueManager.Previous();
        if (index == null) return;
        State.CurrentSong = State.Songs?[index.Value];
        if (player?.IsPlaying ?? true) return;
        player.Play();
        positionTimer.Start();
    }

    [RelayCommand]
    private void Next()
    {
        var index = State.CurrentPlaylist?.QueueManager.Next();
        if (index == null) return;
        State.CurrentSong = State.Songs?[index.Value];
        if (player?.IsPlaying ?? true) return;
        player.Play();
        positionTimer.Start();
    }

    [RelayCommand]
    private void Mute()
    {
        isMuted = !isMuted;
        if (isMuted)
        {
            if (Application.Current!.TryGetResource("VolumeMuteIcon", Application.Current.ActualThemeVariant,
                    out var res))
                VolumeIcon = res as Geometry;
        }
        else
        {
            OnVolumeChanged(Volume);
        }

        player?.Muted = isMuted;
    }

    [RelayCommand]
    private void GoTo(object? parameter)
    {
        switch (parameter)
        {
            case string page:
                CurrentViewModel = page switch
                {
                    "settings" => settingsViewModel,
                    _ => CurrentViewModel
                };
                break;
            case PlaylistBinding playlist:
                if (CurrentViewModel is PlaylistViewModel && playlist.Id == State.CurrentPlaylist?.Id)
                {
                    break;
                }

                State.CurrentPlaylist = playlist;
                CurrentViewModel = playlistViewModel;

                break;
        }
    }

    [RelayCommand]
    private void AddPlaylist()
    {
        if (!string.IsNullOrEmpty(Url) && !string.IsNullOrWhiteSpace(Url))
        {
            State.Tasks =
            [
                .. State.Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
                Task.Run(async () =>
                {
                    try
                    {
                        var playlist = await State.Services.AddPlayist(Url, State.Cts.Token);
                        if (playlist != null)
                        {
                            State.Playlists?.Add(State.ToBinding(playlist));
                        }

                        Url = string.Empty;
                    }
                    catch (Exception e)
                    {
                        Message = e.Message;
                    }
                })
            ];
        }
    }

    public void Load()
    {
        State.Tasks.Add(State.Services.Initialize(State.Cts.Token));
    }

    public void Closing()
    {
        State.AppSettings.Volume = Volume;

        AppSettings.GetInstance().Dispose();
        State.AppSettings.Dispose();
        State.Cts.Cancel();
        Task.WhenAll(State.Tasks).GetAwaiter().GetResult();
        State.Cts.Dispose();
    }
}