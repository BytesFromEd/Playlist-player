namespace Infrastructure.Services;

public class ServiceEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}