using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OrbitHub.Models;
using OrbitHub.ViewModels;

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

    // 👇 CAMBIO AQUÍ: sender es "object" en lugar de "object?"
    private void OnGameDoubleTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: Game game } && DataContext is MainWindowViewModel vm)
        {
            vm.LaunchGameCommand.Execute(game);
            e.Handled = true;
        }
    }
}