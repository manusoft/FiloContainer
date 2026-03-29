using FiloExplorer.Helpers;
using FiloExplorer.Models;
using ManuHub.Filo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Windows.Storage;
using Windows.Storage.FileProperties;

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
                IsDirectory = false,
                ImageSource = GetFileThumbnail(f.Path),
            });
        }
    }

    private BitmapImage? GetFileThumbnail(string path)
    {
        try
        {
            var storageFile = StorageFile.GetFileFromPathAsync(path).GetAwaiter().GetResult();
            var storageItemThumbnail = storageFile.GetThumbnailAsync(ThumbnailMode.ListView, 32, ThumbnailOptions.UseCurrentScale).GetAwaiter().GetResult();
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(storageItemThumbnail);
            return bitmapImage;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private BitmapImage? GetFolderThumbnail(string path)
    {
        try
        {
            var storageFolder = StorageFolder.GetFolderFromPathAsync(path).GetAwaiter().GetResult();
            var storageItemThumbnail = storageFolder.GetThumbnailAsync(ThumbnailMode.ListView, 32, ThumbnailOptions.UseCurrentScale).GetAwaiter().GetResult();
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(storageItemThumbnail);
            return bitmapImage;
        }
        catch (Exception)
        {
            return null;
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
                IsDirectory = true,
                ImageSource = GetFolderThumbnail(folder.Path)
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
                writer.AddDirectory(item.Path, Path.GetFileName(item.Path));
            else
                writer.AddFile(item.Path, new FileMetadata
                {
                    MimeType = MimeHelper.GetMimeType(item.Path)
                });
        }

        await writer.WriteAsync();

        PendingItems.Clear();
        this.Frame.Navigate(typeof(MainPage));
    }

    // Add Override Support (IMPORTANT)
    //public void AddFileSmart(string path, string? mimeOverride = null)
    //{
    //    var mime = mimeOverride ?? MimeHelper.GetMimeType(path);

    //    writer.AddFile(path, new FileMetadata
    //    {
    //        MimeType = mime
    //    });
    //}


}
