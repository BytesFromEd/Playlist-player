using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Infrastructure.MediaPlayer;

namespace UI.Models;

public partial class PlaylistBinding : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; } = false;

    [ObservableProperty] public partial string Owner { get; set; }

    [ObservableProperty] public partial string Id { get; set; }

    [ObservableProperty] public partial Geometry? Provider { get; set; } = null;

    [ObservableProperty] public partial Playlist Playlist { get; set; }

    [ObservableProperty] public partial Bitmap? Image { get; set; }

    public QueueManager QueueManager;

    public PlaylistBinding(Playlist playlist, Bitmap? image)
    {
        Name = playlist.GetName();
        Id = playlist.GetId();
        Owner = playlist.GetOwner();
        Playlist = playlist;
        Image = image;
        QueueManager = new QueueManager(playlist);

        Dispatcher.UIThread.Post(() =>
        {
            switch (playlist.GetProvider())
            {
                case Core.Models.Enums.Provider.Youtube:
                    if (Application.Current!.TryGetResource("YoutubeIcon", Application.Current.ActualThemeVariant,
                            out var ytIcon))
                        Provider = ytIcon as Geometry;
                    break;
                case Core.Models.Enums.Provider.External:
                    if (Application.Current!.TryGetResource("StorageIcon", Application.Current.ActualThemeVariant,
                            out var diskIcon))
                        Provider = diskIcon as Geometry;
                    break;
                default:
                    throw new NotImplementedException();
            }
        });
    }
}