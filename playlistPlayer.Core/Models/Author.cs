namespace playlistPlayer.Core;

public class Author(string name)
{
    private readonly string name = name;

    public string GetName()
    {
        return name;
    }
}