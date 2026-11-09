using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace OrbitHub.Models;

public partial class Game : ObservableObject
{
    public string Name { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string? CoverPath { get; set; }
    
    [ObservableProperty]
    private bool _isFavorite;

    // Tiempo jugado en minutos
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimePlayedDisplay))]
    private double _timePlayedMinutes;

    // Propiedad calculada para mostrar en la tarjeta (ej: "2h 15m")
    public string TimePlayedDisplay
    {
        get
        {
            if (TimePlayedMinutes < 1) return "Nunca jugado";
            var ts = TimeSpan.FromMinutes(TimePlayedMinutes);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m jugados";
            return $"{ts.Minutes}m jugados";
        }
    }
}