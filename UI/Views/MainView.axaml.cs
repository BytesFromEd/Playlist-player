using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UI.ViewModels;

namespace UI.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
        
        PositionSlider.AddHandler(PointerPressedEvent, StartSeeking, RoutingStrategies.Tunnel);
        PositionSlider.AddHandler(PointerReleasedEvent, EndSeeking, RoutingStrategies.Tunnel);


        Closing += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.Closing();
        };
    }

    private void OnTogglePopup(object sender, RoutedEventArgs e)
    {
        PlaylistPopup.IsOpen = !PlaylistPopup.IsOpen;
    }

    private void StartSeeking(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.IsSeeking = true;
    }

    private void EndSeeking(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.EndSeek();
    }
}