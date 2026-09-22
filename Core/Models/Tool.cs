namespace Core.Models;

public class Tool(string name, string version, string? filename)
{
    public string Name { get; set; } = name;
    public string Version { get; set; } = version;
    public string? Filename { get; set; } = filename;
}