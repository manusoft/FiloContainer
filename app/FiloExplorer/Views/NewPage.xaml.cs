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
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace FiloExplorer.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewPage : Page
{
    private readonly ObservableCollection<PendingItem> _pendingItems = new();

    public NewPage()
    {
        InitializeComponent();
        PendingList.ItemsSource = _pendingItems;

        // Show/hide empty state
        _pendingItems.CollectionChanged += (s, e) =>
        {
            EmptyStateGrid.Visibility = _pendingItems.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            UpdateInfoPanel();
        };
    }

    private void UpdateInfoPanel()
    {
        ItemCountText.Text = $"{_pendingItems.Count} item{(_pendingItems.Count != 1 ? "s" : "")}";
        // You can add estimated size calculation here later
    }

    // ====================== Add Files ======================
    private async void AddFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add("*");

        var files = await picker.PickMultipleFilesAsync();
        if (files == null) return;

        foreach (var file in files)
        {
            var item = new PendingItem
            {
                Path = file.Path,
                //Name = file.DisplayName + file.FileType,
                IsDirectory = false,
                Thumbnail = await GetFileThumbnailAsync(file.Path)
            };

            _pendingItems.Add(item);
        }
    }

    // ====================== Add Folder ======================
    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) return;

        var item = new PendingItem
        {
            Path = folder.Path,
            //Name = folder.DisplayName,
            IsDirectory = true,
            Thumbnail = await GetFolderThumbnailAsync(folder.Path)
        };

        _pendingItems.Add(item);
    }

    // ====================== Thumbnail Helper (Async) ======================
    private async Task<BitmapImage?> GetFileThumbnailAsync(string path)
    {
        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(path);
            var storageItemThumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.ListView, 32, ThumbnailOptions.UseCurrentScale);
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(storageItemThumbnail);
            return bitmapImage;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<BitmapImage?> GetFolderThumbnailAsync(string path)
    {
        try
        {
            var storageFolder = await StorageFolder.GetFolderFromPathAsync(path);
            var storageItemThumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.ListView, 32, ThumbnailOptions.UseCurrentScale);
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(storageItemThumbnail);
            return bitmapImage;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ====================== Create Archive ======================
    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingItems.Count == 0)
        {
            await ShowMessageAsync("No items", "Please add at least one file or folder.");
            return;
        }

        try
        {
            var picker = new FileSavePicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.DefaultFileExtension =".filo";
            picker.FileTypeChoices.Add("Filo Archive", new[] { ".filo" });
            picker.SuggestedFileName = "NewArchive.filo";
            picker.CommitButtonText = "Save File";
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFolder = "";

            // Show the picker dialog
            var result = await picker.PickSaveFileAsync();

            if (result != null)
            {
                string savePath = result.Path;

                var writer = new FiloWriter(savePath)
                           .WithPassword("1234");

                foreach (var item in _pendingItems)
                {
                    if (item.IsDirectory)
                    {
                        writer.AddDirectory(item.Path, Path.GetFileName(item.Path));
                    }
                    else
                    {
                        writer.AddFile(item.Path, new FileMetadata
                        {
                            MimeType = MimeHelper.GetMimeType(item.Path)
                        });
                    }
                }

                await writer.WriteAsync();

                await ShowMessageAsync("Success", "Filo archive created successfully!");
                _pendingItems.Clear();
                this.Frame.Navigate(typeof(MainPage));
            }
            else
            {
                await ShowMessageAsync("Error", "File save canceled.!");
            }            
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Error", $"Failed to create archive:\n{ex.Message}");
        }
    }

    // ====================== Remove & Clear ======================
    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (PendingList.SelectedItem is PendingItem item)
        {
            _pendingItems.Remove(item);
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        _pendingItems.Clear();
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }


}
