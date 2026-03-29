using FiloExplorer.Models;
using ManuHub.Filo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FiloExplorer.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewPage : Page
{
    ObservableCollection<PendingItem> PendingItems = new();

    public NewPage()
    {
        InitializeComponent();
        PendingList.ItemsSource = PendingItems;
    }

    private async void AddFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);

        picker.FileTypeFilter.Add("*");

        var files = await picker.PickMultipleFilesAsync();

        foreach (var f in files)
        {
            PendingItems.Add(new PendingItem
            {
                Path = f.Path,
                IsDirectory = false
            });
        }
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);

        var folder = await picker.PickSingleFolderAsync();

        if (folder != null)
        {
            PendingItems.Add(new PendingItem
            {
                Path = folder.Path,
                IsDirectory = true
            });
        }
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        var writer = new FiloWriter(@"c:\downloads\output.filo")
            .WithPassword("1234");

        foreach (var item in PendingItems)
        {
            if (item.IsDirectory)
                writer.AddDirectory(item.Path, item.Path);
            else
                writer.AddFile(item.Path, new FileMetadata { MimeType ="image/jpg" });
        }

        await writer.WriteAsync();

        PendingItems.Clear();
    }

}
