using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;

namespace UI.Models;

public partial class SongBinding : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial string Artist { get; set; }
    [ObservableProperty] public partial Geometry? Provider { get; set; } = null;
    [ObservableProperty] public partial string Duration { get; set; }
    [ObservableProperty] public partial Bitmap? Image { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }

    public readonly Song Song;

    public SongBinding(Song song, Bitmap? image, string duration)
    {
        Title = song.GetTitle();
        Artist = song.GetArtist();
        Duration = duration;
        Image = image;
        IsSelected = false;
        Song = song;
        Dispatcher.UIThread.Post(() =>
        {
            switch (song.GetProvider())
            {
                case Core.Models.Enums.Provider.Youtube:
                    if (Application.Current!.TryGetResource("YoutubeIcon", Application.Current.ActualThemeVariant,
                            out var ytIcon))
                        Provider = ytIcon as Geometry;
                    break;
                case Core.Models.Enums.Provider.External:
                    if (Application.Current!.TryGetResource("DiskIcon", Application.Current.ActualThemeVariant,
                            out var diskIcon))
                        Provider = diskIcon as Geometry;
                    break;
                default:
                    throw new NotImplementedException();
            }
        });
    }
}