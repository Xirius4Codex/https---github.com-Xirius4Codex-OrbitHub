using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _showFavoritesOnly;
    [ObservableProperty] private bool _sortAscending = true;
    
    [ObservableProperty] private bool _isLoading = true; 

    public bool ShowWelcomeScreen => !IsLoading && !HasLibrary;

    private readonly ObservableCollection<Game> _allGames = new();
    public ObservableCollection<Game> Games { get; } = new();

    public MainWindowViewModel()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await Task.Delay(2500);

        var saved = _library.LoadSavedGames();
        if (saved.Count > 0)
        {
            foreach (var g in saved) _allGames.Add(g);
            LibraryPath = _library.LibraryPath ?? "";
            HasLibrary = true;
            UpdateFilteredGames();
        }

        IsLoading = false;
    }

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(ShowWelcomeScreen));
    partial void OnHasLibraryChanged(bool value) => OnPropertyChanged(nameof(ShowWelcomeScreen));

    partial void OnSearchTextChanged(string value) => UpdateFilteredGames();
    partial void OnShowFavoritesOnlyChanged(bool value) => UpdateFilteredGames();

    private void UpdateFilteredGames()
    {
        Games.Clear();
        var filtered = _allGames.AsEnumerable();
        
        if (ShowFavoritesOnly) 
            filtered = filtered.Where(g => g.IsFavorite);
        
        if (!string.IsNullOrWhiteSpace(SearchText)) 
            filtered = filtered.Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        var ordered = SortAscending 
            ? filtered.OrderBy(g => g.Name) 
            : filtered.OrderByDescending(g => g.Name);

        foreach (var g in ordered) Games.Add(g);
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

        var scanned = _library.ScanLibrary(path);
        _allGames.Clear();
        foreach (var g in scanned) _allGames.Add(g);

        _library.Save(path, scanned);
        HasLibrary = true;
        SearchText = ""; 
        ShowFavoritesOnly = false;
        UpdateFilteredGames();
    }

    [RelayCommand]
    private void Rescan()
    {
        if (string.IsNullOrEmpty(LibraryPath)) return;
        var scanned = _library.ScanLibrary(LibraryPath);
        _allGames.Clear();
        foreach (var g in scanned) _allGames.Add(g);
        _library.Save(LibraryPath, scanned);
        UpdateFilteredGames();
    }

    [RelayCommand]
    private void ToggleFavorite(Game? game)
    {
        if (game is null) return;
        game.IsFavorite = !game.IsFavorite;
        _library.Save(LibraryPath, _allGames.ToList());
        UpdateFilteredGames();
    }

    [RelayCommand]
    private void ShowAll() => ShowFavoritesOnly = false;

    [RelayCommand]
    private void ShowFavorites() => ShowFavoritesOnly = true;

    [RelayCommand]
    private void ToggleSortOrder()
    {
        SortAscending = !SortAscending;
        UpdateFilteredGames();
    }

    // MODIFICADO: Ahora es async Task para esperar a que el juego se cierre
    [RelayCommand]
    private async Task LaunchGame(Game? game)
    {
        if (game is null || string.IsNullOrEmpty(game.ExecutablePath)) return;
        if (!System.IO.File.Exists(game.ExecutablePath)) return;

        try
        {
            var process = Process.Start(new ProcessStartInfo 
            { 
                FileName = game.ExecutablePath, 
                WorkingDirectory = game.FolderPath, 
                UseShellExecute = true 
            });

            if (process != null)
            {
                await Task.Run(() => process.WaitForExit());

                var elapsed = process.ExitTime - process.StartTime;
                game.TimePlayedMinutes += elapsed.TotalMinutes;

                _library.Save(LibraryPath, _allGames.ToList());
                UpdateFilteredGames();
            }
        }
        catch (Exception ex) { Console.WriteLine($"Error al lanzar: {ex.Message}"); }
    }

    [RelayCommand]
    private void OpenFolder(Game? game)
    {
        if (game is null || string.IsNullOrEmpty(game.FolderPath)) return;
        if (!System.IO.Directory.Exists(game.FolderPath)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = game.FolderPath, UseShellExecute = true });
        }
        catch (Exception ex) { Console.WriteLine($"Error al abrir carpeta: {ex.Message}"); }
    }

    public void UpdateGameCover(Game game, string newImagePath)
    {
        if (game is null || string.IsNullOrEmpty(newImagePath)) return;
        
        var cachedPath = _library.CopyImageToCache(newImagePath, game.Name);
        game.CoverPath = cachedPath;
        
        _library.Save(LibraryPath, _allGames.ToList());
        UpdateFilteredGames();
    }
}