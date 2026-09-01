using UI.Services;

namespace UI.ViewModels.DesignModels;

public class MainViewModelDesign() : MainViewModel(State, new PlaylistViewModel(State), new SettingsViewModel())
{
    private new static readonly State State = new();
}