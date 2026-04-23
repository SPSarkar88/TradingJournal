using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TradingJournal.ViewModels;

namespace TradingJournal.Views;

public partial class JournalView : UserControl
{
    public JournalView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not JournalViewModel viewModel)
        {
            return;
        }

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(JournalViewModel.PreTradeNotes))
            {
                SetDocumentText(PreTradeEditor, viewModel.PreTradeNotes);
            }

            if (args.PropertyName == nameof(JournalViewModel.PostTradeReview))
            {
                SetDocumentText(PostTradeEditor, viewModel.PostTradeReview);
            }
        };

        SetDocumentText(PreTradeEditor, viewModel.PreTradeNotes);
        SetDocumentText(PostTradeEditor, viewModel.PostTradeReview);
    }

    private void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not JournalViewModel viewModel)
        {
            return;
        }

        viewModel.PreTradeNotes = new TextRange(PreTradeEditor.Document.ContentStart, PreTradeEditor.Document.ContentEnd).Text.Trim();
        viewModel.PostTradeReview = new TextRange(PostTradeEditor.Document.ContentStart, PostTradeEditor.Document.ContentEnd).Text.Trim();
    }

    private static void SetDocumentText(RichTextBox box, string text)
    {
        if (new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text.Trim() == text.Trim())
        {
            return;
        }

        box.Document.Blocks.Clear();
        box.Document.Blocks.Add(new Paragraph(new Run(text)));
    }
}
