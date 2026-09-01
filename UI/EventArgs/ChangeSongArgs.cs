namespace UI.ViewModels.Models;

public class ChangeSongArgs(UI.Models.SongBinding songBinding, bool changeIndex) : SongEventArgs
{
    public readonly UI.Models.SongBinding SongBinding = songBinding;
    public readonly bool ChangeIndex = changeIndex;
}