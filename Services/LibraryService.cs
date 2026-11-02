using System;
using System.Collections.Generic;
using System.Drawing;
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
    private static readonly string CoversDir = Path.Combine(AppDataDir, "covers");

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

            var gameName = Path.GetFileName(folder);
            var coverPath = FindCoverImage(folder);

            // Si no hay imagen local, intentamos extraer el icono del .exe
            if (coverPath is null)
            {
                coverPath = ExtractExeIcon(exe, gameName);
            }

            games.Add(new Game
            {
                Name = gameName,
                FolderPath = folder,
                ExecutablePath = exe,
                CoverPath = coverPath
            });
        }

        return games.OrderBy(g => g.Name).ToList();
    }

    private static string? FindMainExecutable(string folder)
    {
        var exes = Directory.GetFiles(folder, "*.exe", SearchOption.AllDirectories);
        if (exes.Length == 0) return null;

        var folderName = Path.GetFileName(folder);
        var match = exes.FirstOrDefault(e =>
            Path.GetFileNameWithoutExtension(e)
                .Equals(folderName, StringComparison.OrdinalIgnoreCase));

        return match ?? exes[0];
    }

    private static string? FindCoverImage(string folder)
    {
        string[] commonNames = { "cover", "poster", "folder", "boxart", "art", "logo" };
        string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };

        foreach (var name in commonNames)
        {
            foreach (var ext in extensions)
            {
                var path = Path.Combine(folder, name + ext);
                if (File.Exists(path)) return path;
            }
        }
        return null;
    }

    private static string? ExtractExeIcon(string exePath, string gameName)
    {
        try
        {
            Directory.CreateDirectory(CoversDir);
            var iconPath = Path.Combine(CoversDir, $"{SanitizeFileName(gameName)}.png");
            
            // Si ya existe el icono extraído, lo reutilizamos
            if (File.Exists(iconPath)) return iconPath;

            using var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon is null) return null;

            using var bitmap = icon.ToBitmap();
            bitmap.Save(iconPath, System.Drawing.Imaging.ImageFormat.Png);
            
            return iconPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extrayendo icono de {gameName}: {ex.Message}");
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalid.Contains(c)));
    }

    private class LibraryData
    {
        public string? LibraryPath { get; set; }
        public List<Game> Games { get; set; } = new();
    }
}