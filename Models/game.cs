namespace OrbitHub.Models;

public class Game
{
    public string Name { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public bool IsFavorite { get; set; }
}