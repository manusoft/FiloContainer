using FiloExplorer.Models;
using ManuHub.Filo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;

namespace FiloExplorer.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ViewPage : Page
{
    private FiloReader _reader;
    private readonly List<FiloFileInfo> _allFiles = new();
    private readonly ObservableCollection<ContainerItem> _items = new();

    private string _currentFolder = "";
    private byte[] _key;

    public ObservableCollection<string> BreadcrumbItems { get; } = new();

    public ViewPage()
    {
        InitializeComponent();
        FileList.ItemsSource = _items;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            string path = e.Parameter as string;
            _reader = new FiloReader(path);
            await _reader.InitializeAsync();

            _key = _reader.Header.Encryption == "AES256"
                ? _reader.DeriveKey("1234")  // Consider making password configurable
                : null;

            _allFiles.Clear();
            _allFiles.AddRange(_reader.ListFiles());

            RefreshView();
            UpdateBreadcrumb();
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog
            ContentDialog dialog = new()
            {
                Title = "Error",
                Content = $"Failed to open archive: {ex.Message}",
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }
    }

    void RefreshView()
    {
        _items.Clear();
        var folders = new HashSet<string>();

        foreach (var file in _allFiles)
        {
            if (!file.Directory.StartsWith(_currentFolder, StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = file.Directory.Substring(_currentFolder.Length).Trim('/');

            if (string.IsNullOrEmpty(relative))
            {
                // File in current folder
                _items.Add(new ContainerItem
                {
                    Name = file.Name,
                    FullPath = file.Path,
                    IsFolder = false,
                    Size = file.FileSize,
                    MimeType = file.MimeType,
                    Encrypted = file.Encrypted
                });
            }
            else
            {
                // Folder
                folders.Add(relative.Split('/')[0]);
            }
        }

        // Add folders at the top
        foreach (var folder in folders.OrderBy(f => f))
        {
            _items.Insert(0, new ContainerItem
            {
                Name = folder,
                FullPath = string.IsNullOrEmpty(_currentFolder)
                    ? folder
                    : $"{_currentFolder}/{folder}",
                IsFolder = true
            });
        }
    }

    private void UpdateBreadcrumb()
    {
        BreadcrumbItems.Clear();

        BreadcrumbItems.Add("Home");

        if (string.IsNullOrEmpty(_currentFolder))
            return;

        var parts = _currentFolder.Split('/');

        string accumulatedPath = "";

        foreach (var part in parts)
        {
            accumulatedPath = string.IsNullOrEmpty(accumulatedPath)
                ? part
                : $"{accumulatedPath}/{part}";

            BreadcrumbItems.Add(part);
        }
    }

    private async void FileList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ContainerItem item) return;

        if (item.IsFolder)
        {
            _currentFolder = item.FullPath;
            RefreshView();
            UpdateBreadcrumb();
        }
        else
        {
            await PreviewFileAsync(item);
        }
    }

    // -------- Preview --------
    private async Task PreviewFileAsync(ContainerItem item)
    {
        try
        {
            // Hide all previews first
            PreviewImage.Visibility = Visibility.Collapsed;
            MediaPlayerElement.Visibility = Visibility.Collapsed;
            NoPreviewPanel.Visibility = Visibility.Collapsed;

            using var filoStream = new FiloStream(_reader, item.FullPath, _key);
            var ras = await ToRandomAccessStreamAsync(filoStream);

            if (item.MimeType?.StartsWith("image") == true)
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(ras);
                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
            }
            else if (item.MimeType?.StartsWith("video") == true ||
                     item.MimeType?.StartsWith("audio") == true)
            {
                MediaPlayerElement.Source = MediaSource.CreateFromStream(ras, item.MimeType);
                MediaPlayerElement.Visibility = Visibility.Visible;
                MediaPlayerElement.MediaPlayer?.Play();
            }
            else
            {
                // TODO: Add text preview, PDF, etc.
                NoPreviewPanel.Visibility = Visibility.Visible;
            }

            PreviewHeaderText.Text = $"Preview - {item.Name}";
        }
        catch (Exception ex)
        {
            // Show error in preview area
            PreviewHeaderText.Text = "Preview Error";
            NoPreviewPanel.Visibility = Visibility.Visible;
        }
    }

    private async Task<IRandomAccessStream> ToRandomAccessStreamAsync(Stream stream)
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream.AsRandomAccessStream();
    }

    // Toolbar Handlers
    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFolder)) return;

        var lastSlash = _currentFolder.LastIndexOf('/');
        _currentFolder = lastSlash > 0
            ? _currentFolder.Substring(0, lastSlash)
            : "";

        RefreshView();
        UpdateBreadcrumb();
    }

    // -------- Extract --------
    private async void ExtractButton_Click(object sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ContainerItem item || item.IsFolder)
        {
            // Show message: select a file
            return;
        }

        var folderPicker = new FolderPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            await _reader.ExtractFileAsync(item.FullPath, folder.Path, _key);
            // Show success notification
        }
    }

    private async void ExtractAllButton_Click(object sender, RoutedEventArgs e)
    {
        // Similar to above but extract all files
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index == 0)
        {
            _currentFolder = "";
        }
        else
        {
            // Rebuild path up to clicked item
            var pathParts = new List<string>();
            for (int i = 1; i <= args.Index; i++)
            {
                pathParts.Add(BreadcrumbItems[i]);
            }
            _currentFolder = string.Join("/", pathParts);
        }

        RefreshView();
        UpdateBreadcrumb(); // Optional: trim breadcrumb after click
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshView();
    }
}
