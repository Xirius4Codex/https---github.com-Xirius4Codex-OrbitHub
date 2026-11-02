using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OrbitHub.Models;
using OrbitHub.ViewModels;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OrbitHub.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private async void OnSelectLibraryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            await vm.SelectLibraryAsync(this);
    }

    private void OnGameDoubleTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: Game game } && DataContext is MainWindowViewModel vm)
        {
            vm.LaunchGameCommand.Execute(game);
            e.Handled = true;
        }
    }

    // NUEVO: Handler para el botón de cambiar carátula
    private async void OnChangeCoverClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: Game game } && DataContext is MainWindowViewModel vm)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Selecciona una imagen para la carátula",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            if (files.Count > 0)
            {
                var localPath = files[0].Path.LocalPath;
                vm.UpdateGameCover(game, localPath);
            }
        }
    }
}

// Convertidores
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try { return new Avalonia.Media.Imaging.Bitmap(path); }
            catch { return null; }
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToTabColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b && b) ? Avalonia.Media.Brush.Parse("#4F7CFF") : Avalonia.Media.Brush.Parse("#252A36");
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToTabTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b && b) ? Avalonia.Media.Brush.Parse("#FFFFFF") : Avalonia.Media.Brush.Parse("#8A93A6");
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}