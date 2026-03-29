using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FiloExplorer.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(NewPage));
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".filo");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            Frame.Navigate(typeof(ViewPage), file.Path);
        }
    }
}
