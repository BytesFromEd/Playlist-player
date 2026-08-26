namespace UI.ViewModels.Models;

public class ChangeSongArgs(SongBinding songBinding, bool changeIndex) : SongEventArgs
{
    public readonly SongBinding SongBinding = songBinding;
    public readonly bool ChangeIndex = changeIndex;
}