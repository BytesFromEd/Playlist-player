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
using Infrastructure.Services;
using UI.ViewModels.Models;


namespace UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial ViewModelBase? CurrentViewModel { get; set; }
    [ObservableProperty] public partial string Url { get; set; } = string.Empty;
    [ObservableProperty] public partial string Message { get; set; } = string.Empty;

    [ObservableProperty] public partial float Volume { get; set; } = AppSettings.GetInstance().Volume;
    [ObservableProperty] public partial long Progress { get; set; } = 0;
    [ObservableProperty] public partial long Lenght { get; set; } = 0;

    [ObservableProperty] public partial Geometry? VolumeIcon { get; set; }
    [ObservableProperty] public partial Geometry? PlayIcon { get; set; }

    private readonly SettingsViewModel settingsViewModel = new();
    public static readonly Services Services = Services.GetInstance();

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

    public MainViewModel()
    {
        Tasks =
        [
            .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
            Task.Run(() =>
            {
                Services.CreateTable();

                var playlists = Services.GetPlaylists().Select(ToBinding).ToList();

                CurrentViewModel = playlists.Count != 0
                    ? new PlaylistViewModel(playlists.First())
                    : settingsViewModel;

                Playlists = new ObservableCollection<PlaylistBinding>([.. playlists]);
            })
        ];
/*
        PlaylistViewModel.OnMessage += (_, args) => { Message = args.Message; };

        Services.OnServiceMessage += (_, args) => { Message = args.Message; };
*/
        player = new MediaPlayer();

        player.OnStopped += (_, args) =>
        {
            if (args.Exception != null)
                Message = args.Exception.Message;

            Next();
        };

        positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        positionTimer.Tick += (_, _) =>
        {
            if (player is { IsPlaying: true })
                PositionChanged?.Invoke(this, player.Position);
        };
        PositionChanged += (_, args) => { Progress = args; };

        OnNextSong += (_, args) =>
        {
            if (args is not ChangeSongArgs change) return;

            var lastStatePlay = player.IsPlaying;

            CurrentSong = change.SongBinding;

            positionTimer.Stop();
            player.SetSong(Path.Combine(AppSettings.GetInstance().AppFolder, "songs", change.SongBinding.Song.GetFile()));
            Lenght = player.Lenght;
            IsSeeking = false;
            Progress = 0;

            if (!lastStatePlay) return;

            player.Play();
            positionTimer.Start();
        };

        OnVolumeChanged(Volume);

        PlayIcon = Application.Current?.Resources["PlayIcon"] as Geometry;
    }


    partial void OnVolumeChanged(float value)
    {
        isMuted = false;

        if (value < 0.25)
        {
            VolumeIcon = Application.Current?.Resources["VolumeNoneIcon"] as Geometry;
        }
        else if (value < 0.5)
        {
            VolumeIcon = Application.Current?.Resources["VolumeLowIcon"] as Geometry;
        }
        else
        {
            VolumeIcon = Application.Current?.Resources["VolumeFullIcon"] as Geometry;
        }

        player?.Volume = value;
    }

    public void EndSeek()
    {
        IsSeeking = false;
        if (player is null) return;

        player.Position = Progress;
        player.Play();
        positionTimer.Start();

        PlayIcon = Application.Current?.Resources["PauseIcon"] as Geometry;
    }

    [RelayCommand]
    private void Play()
    {
        player?.PlayStop();

        PlayIcon = player?.IsPlaying ?? true
            ? Application.Current?.Resources["PauseIcon"] as Geometry
            : Application.Current?.Resources["PlayIcon"] as Geometry;

        if (player?.IsPlaying ?? false)
        {
            positionTimer.Start();
        }
        else
        {
            positionTimer.Stop();
        }
    }

    [RelayCommand]
    private void Prev()
    {
        OnNextSong?.Invoke(this, new PrevSongArgs());
    }

    [RelayCommand]
    private void Next()
    {
        OnNextSong?.Invoke(this, new NextSongArgs());
    }

    [RelayCommand]
    private void Mute()
    {
        isMuted = !isMuted;
        if (isMuted)
            VolumeIcon = Application.Current?.Resources["VolumeMuteIcon"] as Geometry;
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
                if (CurrentViewModel is PlaylistViewModel vm && vm.Playlist?.Id == playlist.Id)
                {
                    break;
                }

                CurrentViewModel = new PlaylistViewModel(playlist);

                break;
        }
    }

    [RelayCommand]
    private void AddPlaylist()
    {
        if (!string.IsNullOrEmpty(Url) && !string.IsNullOrWhiteSpace(Url))
        {
            Tasks =
            [
                .. Tasks.Where(x => x is { IsCanceled: false, IsCompleted: false }),
                Task.Run(async () =>
                {
                    try
                    {
                        var playlist = await Services.AddPlayist(Url, Cts.Token);
                        if (playlist != null)
                        {
                            Playlists.Add(ToBinding(playlist));
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


    public void Closing()
    {
        AppSettings.GetInstance().Volume = Volume;
    }
}