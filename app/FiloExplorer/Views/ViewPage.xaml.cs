using FiloExplorer.Models;
using ManuHub.Filo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
using static FiloExplorer.Helpers.MsgHelper;

namespace FiloExplorer.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ViewPage : Page
{
    //private readonly INotificationService? _notification;
    private FiloReader? _reader;
    private readonly List<FiloFileInfo> _allFiles = new();
    private readonly ObservableCollection<ContainerItem> _items = new();
    private string _currentFolder = "";
    private byte[]? _key;
    private string? _archivePath;

    public ObservableCollection<string> BreadcrumbItems { get; } = new();

    public ViewPage()
    {
        InitializeComponent();
        FileList.ItemsSource = _items;
        //_notification = App.Services.GetService<INotificationService>();

        this.Loaded += ViewPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _archivePath = e.Parameter as string;

        if (string.IsNullOrEmpty(_archivePath))
        {
            Frame.GoBack();
        }

        // Do nothing heavy here — wait for Loaded
    }

    private async void ViewPage_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= ViewPage_Loaded;   // Prevent multiple calls

        if (string.IsNullOrEmpty(_archivePath)) return;

        await OpenArchiveWithPasswordAsync(_archivePath);
    }

    private async Task OpenArchiveWithPasswordAsync(string path)
    {
        try
        {
            _reader = new FiloReader(path);
            await _reader.InitializeAsync();

            // === Password Handling for Filo 1.1.0 ===
            if (_reader.Header.Encryption == "AES256")
            {
                string? password = await ShowPasswordDialogAsync(this.XamlRoot);

                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowToast("Operation Cancelled", "Password is required for this encrypted container.");
                    // await ShowMessageDialogAsync("Operation Cancelled", "Password is required for this encrypted container.", this.XamlRoot);
                    Frame.GoBack();
                    return;
                }

                _key = _reader.DeriveKey(password); // If password is wrong, exception will be thrown Invakid Password
            }
            else
            {
                _key = null; // No encryption
            }

            // Load files
            _allFiles.Clear();
            _allFiles.AddRange(_reader.ListFiles());

            RefreshView();
            UpdateBreadcrumb();

            // Optional: Show encryption status in UI
            if (_key != null)
                PreviewHeaderText.Text = "Preview (Encrypted Container)";
        }
        catch (Exception ex)
        {
            ShowToast("Failed to Open Container", ex.Message);
            //await ShowMessageDialogAsync("Failed to Open Container", ex.Message, this.XamlRoot);
            Frame.GoBack();
        }
    }

    private void RefreshView()
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
            ShowToast("No file selected", "Please select a file to extract.");
            return;
        }

        var folderPicker = new FolderPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder == null) return;

        try
        {
            var itemPath = Path.Combine(folder.Path, item.FullPath);
            await _reader.ExtractFileAsync(item.FullPath, itemPath, _key);
        }
        catch (Exception ex)
        {
        }
    }

    private async void ExtractAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            ShowToast("Nothing to extract", "There are no files or folders in the current view to extract.");
            return;
        }

        var folderPicker = new FolderPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder == null) return;

        foreach (var item in _items)
        {
            if (item.IsFolder)
            {
                try
                {
                    await _reader.ExtractDirectoryAsync(item.FullPath, Path.Combine(folder.Path, item.Name), _key);
                }
                catch (Exception ex)
                { }
            }
            else
            {
                try
                {
                    await _reader.ExtractFileAsync(item.FullPath, Path.Combine(folder.Path, item.Name), _key);
                }
                catch (Exception ex)
                { }
            }
        }
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

    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // going back without confirmation is a bad idea, not unsaved changes, navigate to main page
        var result = await ShowConfirmationDialogAsync("Go Back", "Are you sure you want to go back to home page?", this.XamlRoot);
        if (!result) return;

        if (Frame.CanGoBack)
        {
            PreviewImage.Source = null;
            MediaPlayerElement.Source = null;
            Frame.GoBack();
        }
    }
}
