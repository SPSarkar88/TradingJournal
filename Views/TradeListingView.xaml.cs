using System.ComponentModel;
using System.Windows.Controls;
using TradingJournal.ViewModels;

namespace TradingJournal.Views;

public partial class TradeListingView : UserControl
{
    public TradeListingView()
    {
        InitializeComponent();
    }

    private void TradeGrid_OnSorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not TradeListingViewModel viewModel)
        {
            return;
        }

        e.Handled = true;
        viewModel.SortTradesCommand.Execute(e.Column.SortMemberPath);
        e.Column.SortDirection = viewModel.SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
    }

    private void TradeGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not TradeListingViewModel viewModel)
        {
            return;
        }

        viewModel.TradeSelectedCallback?.Invoke((sender as DataGrid)?.SelectedItem as TradeViewModel);
    }
}
