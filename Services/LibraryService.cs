using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
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
        catch
        {
            try
            {
                var backup = LibraryFile + ".bak";
                if (!File.Exists(backup)) return new List<Game>();
                var data = JsonSerializer.Deserialize<LibraryData>(File.ReadAllText(backup));
                LibraryPath = data?.LibraryPath;
                return data?.Games ?? new List<Game>();
            }
            catch { return new List<Game>(); }
        }
    }

    public void Save(string libraryPath, List<Game> games)
    {
        Directory.CreateDirectory(AppDataDir);
        var data = new LibraryData { LibraryPath = libraryPath, Games = games };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        // Atomic replacement and backup to protect the library from partial writes.
        var temp = LibraryFile + ".tmp";
        File.WriteAllText(temp, json);
        if (File.Exists(LibraryFile))
        {
            File.Copy(LibraryFile, LibraryFile + ".bak", true);
            File.Move(temp, LibraryFile, true);
        }
        else File.Move(temp, LibraryFile);
        LibraryPath = libraryPath;
    }

    public List<Game> ScanLibrary(string libraryPath)
    {
        var games = new List<Game>();
        if (!Directory.Exists(libraryPath)) return games;

        foreach (var folder in Directory.EnumerateDirectories(libraryPath))
        {
            string? exe;
            try { exe = FindMainExecutable(folder); }
            catch (Exception ex) { Console.WriteLine($"Error escaneando {folder}: {ex.Message}"); continue; }
            if (exe is null) continue;

            var gameName = Path.GetFileName(folder);
            var coverPath = FindCoverImage(folder);

            // Si no hay imagen local, usamos el extractor de alta resolución
            if (coverPath is null)
            {
                coverPath = IconExtractor.ExtractHighQualityIcon(exe, gameName);
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
        // Avoid scanning deep caches, redistributables, installers and unrelated tools.
        var candidates = new List<string>();
        var queue = new Queue<(string path, int depth)>();
        queue.Enqueue((folder, 0));
        while (queue.Count > 0 && candidates.Count < 250)
        {
            var (path, depth) = queue.Dequeue();
            try
            {
                candidates.AddRange(Directory.EnumerateFiles(path, "*.exe").Take(40));
                if (depth >= 3) continue;
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    var name = Path.GetFileName(dir).ToLowerInvariant();
                    if (name is "redist" or "_commonredist" or "redistributables" or "installer" or "installers" or "cache" or "crashreporter" or "support" or "tools") continue;
                    queue.Enqueue((dir, depth + 1));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        var folderName = Path.GetFileName(folder);
        var ranked = candidates.Select(exe => new
        {
            Path = exe,
            Score = (Path.GetFileNameWithoutExtension(exe).Equals(folderName, StringComparison.OrdinalIgnoreCase) ? 100 : 0)
                  + (Path.GetDirectoryName(exe)!.Equals(folder, StringComparison.OrdinalIgnoreCase) ? 20 : 0)
                  - (new[] { "setup", "install", "unins", "crash", "report", "config", "launcherhelper", "updater", "unitycrash" }
                    .Any(s => Path.GetFileName(exe).Contains(s, StringComparison.OrdinalIgnoreCase)) ? 200 : 0)
        }).OrderByDescending(x => x.Score).ToList();
        return ranked.Count == 0 || ranked[0].Score < 0 ? null : ranked[0].Path;
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

    private class LibraryData
    {
        public string? LibraryPath { get; set; }
        public List<Game> Games { get; set; } = new();
    }
}

// =====================================================================
// CLASE AUXILIAR PARA EXTRAER ICONOS DE ALTA RESOLUCIÓN (256x256)
// Usa la API nativa de Windows (IShellItemImageFactory)
// =====================================================================
public static class IconExtractor
{
    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItemImageFactory
    {
        void GetImage([In, MarshalAs(UnmanagedType.Struct)] SIZE size, [In] SIIGBF flags, [Out] out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SIZE
    {
        public int cx;
        public int cy;
        public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
    }

    [Flags]
    enum SIIGBF
    {
        RESIZETOFIT = 0x00,
        BIGGERSIZEOK = 0x01,
        MEMORYONLY = 0x02,
        ICONONLY = 0x04,
        THUMBNAILONLY = 0x08,
        INCACHEONLY = 0x10,
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    static extern void SHCreateItemFromParsingName(
        [In] string pszPath,
        [In] IntPtr pbc,
        [In] ref Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory shellItemImageFactory);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    public static string? ExtractHighQualityIcon(string exePath, string gameName)
    {
        try
        {
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrbitHub");
            var coversDir = Path.Combine(appDataDir, "covers");
            Directory.CreateDirectory(coversDir);

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = string.Concat(gameName.Where(c => !invalidChars.Contains(c)));
            var iconPath = Path.Combine(coversDir, $"{safeName}.png");

            if (File.Exists(iconPath)) return iconPath;

            // Solicitar icono de 256x256
            var size = new SIZE(256, 256);
            var iid = typeof(IShellItemImageFactory).GUID;
            
            SHCreateItemFromParsingName(exePath, IntPtr.Zero, ref iid, out var factory);
            factory.GetImage(size, SIIGBF.ICONONLY | SIIGBF.BIGGERSIZEOK, out var hBitmap);

            if (hBitmap == IntPtr.Zero) return null;

            // Convertir el HBITMAP a un archivo PNG
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            using (var fileStream = File.Create(iconPath))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }

            DeleteObject(hBitmap);
            return iconPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extrayendo icono HQ para {gameName}: {ex.Message}");
            return null;
        }
    }
}