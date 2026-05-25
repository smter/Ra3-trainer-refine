using System.Windows;
using Ra3Trainer.App.ViewModels;

namespace Ra3Trainer.App.Views;

public partial class ReinforcementUnitPickerWindow : Window
{
    public ReinforcementUnitPickerWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private ReinforcementUnitPickerViewModel? ViewModel => DataContext as ReinforcementUnitPickerViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.RequestClose += OnRequestClose;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.RequestClose -= OnRequestClose;
        }
    }

    private void OnRequestClose(bool? dialogResult)
    {
        Dispatcher.Invoke(() =>
        {
            DialogResult = dialogResult ?? false;
        });
    }
}
