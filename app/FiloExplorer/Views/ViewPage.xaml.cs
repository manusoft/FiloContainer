using FiloExplorer.Models;
using ManuHub.Filo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
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
    FiloReader reader;
    List<FiloFileInfo> allFiles = new();
    ObservableCollection<ContainerItem> Items = new();
    string currentFolder = "";
    byte[] key;

    public ViewPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        string path = e.Parameter as string;

        reader = new FiloReader(path);
        await reader.InitializeAsync();

        key = reader.Header.Encryption == "AES256"
            ? reader.DeriveKey("1234")
            : null;

        allFiles = reader.ListFiles().ToList();
        FileList.ItemsSource = Items;

        RefreshView();
    }

    void RefreshView()
    {
        Items.Clear();

        var folders = new HashSet<string>();

        foreach (var file in allFiles)
        {
            if (!file.Directory.StartsWith(currentFolder)) continue;

            var relative = file.Directory.Substring(currentFolder.Length).Trim('/');

            if (string.IsNullOrEmpty(relative))
            {
                Items.Add(new ContainerItem
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
                folders.Add(relative.Split('/')[0]);
            }
        }

        foreach (var folder in folders.OrderBy(x => x))
        {
            Items.Insert(0, new ContainerItem
            {
                Name = folder,
                FullPath = $"{currentFolder}/{folder}".Trim('/'),
                IsFolder = true
            });
           
        }

        UpdateBreadcrumb();
    }

    private async void FileList_ItemClick(object sender, ItemClickEventArgs e)
    {
        var item = e.ClickedItem as ContainerItem;

        if (item.IsFolder)
        {
            currentFolder = item.FullPath;
            RefreshView();
        }
        else
        {
            await PreviewFile(item);
        }
    }

    // -------- Preview --------
    async Task PreviewFile(ContainerItem item)
    {
        var filoStream = new FiloStream(reader, item.FullPath, key);

        if (item.MimeType.StartsWith("image"))
        {
            var ras = await ToRandomAccessStream(filoStream);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(ras);

            PreviewImage.Source = bitmap;
            PreviewImage.Visibility = Visibility.Visible;
            MediaPlayer.Source = null;
            MediaPlayer.AreTransportControlsEnabled = false;
            MediaPlayer.Visibility = Visibility.Collapsed;
        }
        else if (item.MimeType.StartsWith("video") || item.MimeType.StartsWith("audio"))
        {
            var ras = await ToRandomAccessStream(filoStream);
            MediaPlayer.Source = MediaSource.CreateFromStream(ras, item.MimeType);
            MediaPlayer.Visibility = Visibility.Visible;
            MediaPlayer.MediaPlayer.Play();
            MediaPlayer.AreTransportControlsEnabled = true;
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewImage.Source = null;
        }

        // if (mime.StartsWith("image"))
        //    ShowImage();
        // else if (mime.StartsWith("video"))
        //    PlayVideo();
        // else if (mime.StartsWith("audio"))
        //    PlayAudio();
        // else if (mime.StartsWith("text"))
        //    ShowText();
        // else
        //    ShowGeneric();
    }

    async Task<IRandomAccessStream> ToRandomAccessStream(Stream input)
    {
        var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        memory.Position = 0;

        return memory.AsRandomAccessStream();
    }

    // -------- Extract --------
    async Task ExtractSelected(ContainerItem item)
    {
        await reader.ExtractFileAsync(item.FullPath, "output_path", key);
    }

    // -------- Breadcrumb --------


    public ObservableCollection<string> BreadcrumbItems { get; set; } = new();

    void UpdateBreadcrumb()
    {
        BreadcrumbItems.Clear();

        var parts = string.IsNullOrEmpty(currentFolder)
            ? new List<string>()
            : currentFolder.Split('/').ToList();

        string path = "";

        AddCrumb("Home", "");

        foreach (var part in parts)
        {
            path = string.IsNullOrEmpty(path) ? part : $"{path}/{part}";
            AddCrumb(part, path);
        }
    }

    void AddCrumb(string name, string path)
    {
        BreadcrumbBar.ItemClicked += (s, e) =>
        {
            var items = BreadcrumbBar.ItemsSource as ObservableCollection<string>;
            for (int i = items.Count - 1; i >= e.Index + 1; i--)
            {
                items.RemoveAt(i);
                RefreshView();
            }

            currentFolder = path;
        };

        BreadcrumbItems.Add(name);
        BreadcrumbBar.ItemsSource = BreadcrumbItems;
    }
}
