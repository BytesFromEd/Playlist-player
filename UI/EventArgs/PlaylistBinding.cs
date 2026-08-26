using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Core.Models.Enums;
using Infrastructure.MediaPlayer;

namespace UI.ViewModels.Models;

public partial class PlaylistBinding(Playlist playlist, Bitmap? image) : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = playlist.GetName();

    [ObservableProperty] public partial string Owner { get; set; } = playlist.GetOwner();

    [ObservableProperty] public partial string Id { get; set; } = playlist.GetId();

    [ObservableProperty] public partial Provider Provider { get; set; } = playlist.GetProvider();

    [ObservableProperty] public partial Playlist Playlist { get; set; } = playlist;

    [ObservableProperty] public partial Bitmap? Image { get; set; } = image;

    public readonly QueueManager QueueManager = new(playlist);
}