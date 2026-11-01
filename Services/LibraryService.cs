using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OrbitHub.Models;

namespace OrbitHub.Services;

public class LibraryService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrbitHub");

    private static readonly string LibraryFile = Path.Combine(AppDataDir, "library.json");

    public string? LibraryPath { get; private set; }

    public List<Game> LoadSavedGames()
    {
        try
        {
            if (!File.Exists(LibraryFile)) return new List<Game>();
            var json = File.ReadAllText(LibraryFile);
            var data = JsonSerializer.Deserialize<LibraryData>(json);
            LibraryPath = data?.LibraryPath;
            return data?.Games ?? new List<Game>();
        }
        catch { return new List<Game>(); }
    }

    public void Save(string libraryPath, List<Game> games)
    {
        Directory.CreateDirectory(AppDataDir);
        var data = new LibraryData { LibraryPath = libraryPath, Games = games };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(LibraryFile, json);
        LibraryPath = libraryPath;
    }

    public List<Game> ScanLibrary(string libraryPath)
    {
        var games = new List<Game>();
        if (!Directory.Exists(libraryPath)) return games;

        foreach (var folder in Directory.GetDirectories(libraryPath))
        {
            var exe = FindMainExecutable(folder);
            if (exe is null) continue;

            games.Add(new Game
            {
                Name = Path.GetFileName(folder),
                FolderPath = folder,
                ExecutablePath = exe
            });
        }

        return games.OrderBy(g => g.Name).ToList();
    }

    private static string? FindMainExecutable(string folder)
    {
        var exes = Directory.GetFiles(folder, "*.exe", SearchOption.AllDirectories);
        if (exes.Length == 0) return null;

        var folderName = Path.GetFileName(folder);
        // Prioriza un .exe que se llame igual que la carpeta
        var match = exes.FirstOrDefault(e =>
            Path.GetFileNameWithoutExtension(e)
                .Equals(folderName, StringComparison.OrdinalIgnoreCase));

        return match ?? exes[0];
    }

    private class LibraryData
    {
        public string? LibraryPath { get; set; }
        public List<Game> Games { get; set; } = new();
    }
}