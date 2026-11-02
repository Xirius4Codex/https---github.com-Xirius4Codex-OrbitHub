using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using OrbitHub.Models;
using OrbitHub.ViewModels;
using System;
using System.Globalization;
using System.IO;

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
}

public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                return new Avalonia.Media.Imaging.Bitmap(path);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}