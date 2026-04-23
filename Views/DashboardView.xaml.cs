using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LiveChartsCore.SkiaSharpView.WPF;

namespace TradingJournal.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (EquityChartHost.Content is null)
        {
            var cartesianChart = new CartesianChart();
            cartesianChart.SetBinding(CartesianChart.SeriesProperty, new Binding("EquityCurveSeries"));
            EquityChartHost.Content = cartesianChart;
        }

        if (WinLossChartHost.Content is null)
        {
            var pieChart = new PieChart();
            pieChart.SetBinding(PieChart.SeriesProperty, new Binding("WinLossSeries"));
            WinLossChartHost.Content = pieChart;
        }
    }
}
