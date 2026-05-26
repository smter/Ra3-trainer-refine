using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Ra3Trainer.App.ViewModels;

namespace Ra3Trainer.App.Converters;

public class ConnectionStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => Application.Current.TryFindResource("SuccessBrush"),
                ConnectionState.Processing => Application.Current.TryFindResource("WarningBrush"),
                _ => Application.Current.TryFindResource("ErrorBrush")
            };
        }

        return Application.Current.TryFindResource("ErrorBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
