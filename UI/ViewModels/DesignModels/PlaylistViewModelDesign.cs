using UI.Services;

namespace UI.ViewModels.DesignModels;

public class PlaylistViewModelDesign : PlaylistViewModel
{
    private new static readonly State State = new();

    public PlaylistViewModelDesign() : base(State)
    {
        State.CurrentPlaylist = State.ToBinding(State.Services.GetPlaylists()[0]);
    }
}