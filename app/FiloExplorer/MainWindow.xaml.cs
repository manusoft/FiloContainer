using FiloExplorer.Views;
using Microsoft.UI.Xaml;

namespace FiloExplorer;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(MainPage));

        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.Dark;

        // Set the window title
        AppWindow.Title = "Filo Explorer";

        // Set the window size (including borders)
        AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 600));
    }

    private void menuFileNew_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.Navigate(typeof(MainPage));
    }
}
