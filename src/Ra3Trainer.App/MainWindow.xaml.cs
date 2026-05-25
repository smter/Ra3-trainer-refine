using System.Windows;
using Ra3Trainer.App.ViewModels;

namespace Ra3Trainer.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.LoadDefault();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Dispose();
        }
        base.OnClosed(e);
    }
}
