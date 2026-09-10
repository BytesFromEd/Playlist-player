using System.Collections.ObjectModel;
using UI.ViewModels.Settings;

namespace UI.ViewModels;

public partial class SettingsViewModel: ViewModelBase
{
    public ObservableCollection<SettingsTabViewModelBase> Tabs { get; } =
    [
        new GeneralSettingsViewModel(),
        new YoutubeSettingsViewModel()
    ];
}