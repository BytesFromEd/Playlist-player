namespace playlistPlayer.Infrastructure.Ytdlp;

public class YtdlpMessageEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}