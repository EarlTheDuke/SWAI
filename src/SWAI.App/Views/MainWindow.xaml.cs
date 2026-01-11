using System.Windows;
using System.Windows.Controls;

namespace SWAI.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Auto-scroll to bottom when new messages are added
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.Messages.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
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
}
