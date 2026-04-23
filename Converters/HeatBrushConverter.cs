using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TradingJournal.Converters;

public sealed class HeatBrushConverter : IValueConverter
{
    private static readonly Brush ProfitBrush = new SolidColorBrush(Color.FromRgb(208, 238, 214));
    private static readonly Brush LossBrush = new SolidColorBrush(Color.FromRgb(245, 212, 206));
    private static readonly Brush NeutralBrush = new SolidColorBrush(Color.FromRgb(233, 220, 197));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Profit" => ProfitBrush,
            "Loss" => LossBrush,
            _ => NeutralBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
