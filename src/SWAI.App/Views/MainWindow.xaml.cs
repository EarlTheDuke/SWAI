using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SWAI.App.ViewModels;
using SWAI.Core.Models.Preview;

namespace SWAI.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Wire up auto-scroll after DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Messages.CollectionChanged += (s, args) =>
            {
                if (args.NewItems != null)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        MessagesScrollViewer.ScrollToEnd();
                    });
                }
            };
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        
        // Focus the input textbox
        InputTextBox.Focus();
    }

    /// <summary>
    /// Handle click on preview history item
    /// </summary>
    private void PreviewHistoryItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && 
            element.DataContext is CommandPreviewResult preview &&
            DataContext is MainViewModel vm)
        {
            vm.LoadPreviewFromHistory(preview);
        }
    }
}
