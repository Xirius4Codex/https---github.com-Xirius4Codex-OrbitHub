using CommunityToolkit.Mvvm.ComponentModel;

namespace OrbitHub.Models;

public partial class Game : ObservableObject
{
    public string Name { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string? CoverPath { get; set; }
    
    [ObservableProperty]
    private bool _isFavorite;
}