using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitHub.Models;
using OrbitHub.Services;

namespace OrbitHub.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LibraryService _library = new();

    [ObservableProperty] private bool _hasLibrary;
    [ObservableProperty] private string _libraryPath = "";

    public ObservableCollection<Game> Games { get; } = new();

    public MainWindowViewModel()
    {
        var saved = _library.LoadSavedGames();
        if (saved.Count > 0)
        {
            foreach (var g in saved) Games.Add(g);
            LibraryPath = _library.LibraryPath ?? "";
            HasLibrary = true;
        }
    }

    public async Task SelectLibraryAsync(Window window)
    {
        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Selecciona tu biblioteca de juegos",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        var path = folders[0].Path.LocalPath;
        LibraryPath = path;

        var scanned = await Task.Run(() => _library.ScanLibrary(path));
        Games.Clear();
        foreach (var g in scanned) Games.Add(g);

        _library.Save(path, scanned);
        HasLibrary = true;
    }

    [RelayCommand]
    private async Task Rescan()
    {
        if (string.IsNullOrEmpty(LibraryPath)) return;

        var scanned = await Task.Run(() => _library.ScanLibrary(LibraryPath));
        Games.Clear();
        foreach (var g in scanned) Games.Add(g);

        _library.Save(LibraryPath, scanned);
    }

    [RelayCommand]
    private void LaunchGame(Game? game)
    {
        if (game is null || string.IsNullOrEmpty(game.ExecutablePath)) return;
        if (!System.IO.File.Exists(game.ExecutablePath)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = game.ExecutablePath,
                WorkingDirectory = System.IO.Path.GetDirectoryName(game.ExecutablePath) ?? game.FolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al lanzar: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenFolder(Game? game)
    {
        if (game is null || string.IsNullOrEmpty(game.FolderPath)) return;
        if (!System.IO.Directory.Exists(game.FolderPath)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = game.FolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al abrir carpeta: {ex.Message}");
        }
    }
}