using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Core.Models.Enums;

namespace UI.Models;

public partial class SongBinding(Song song, Bitmap? image, string duration) : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; } = song.GetTitle();
    [ObservableProperty] public partial string Artist { get; set; } = song.GetArtist();
    [ObservableProperty] public partial Provider Provider { get; set; } = song.GetProvider();
    [ObservableProperty] public partial string Duration { get; set; } = duration;
    [ObservableProperty] public partial Bitmap? Image { get; set; } = image;
    [ObservableProperty] public partial bool IsSelected { get; set; } = false;

    public Song Song = song;
}