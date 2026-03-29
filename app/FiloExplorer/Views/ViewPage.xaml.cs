using FiloExplorer.Models;
using ManuHub.Filo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace FiloExplorer.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ViewPage : Page
{
    FiloReader reader;
    IReadOnlyList<FiloFileInfo> allFiles;
    string currentFolder = "";

    ObservableCollection<ContainerItem> Items = new();

    public ViewPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        string path = e.Parameter as string;

        reader = new FiloReader(path);
        await reader.InitializeAsync();

        allFiles = reader.ListFiles();

        FileList.ItemsSource = Items;

        RefreshView();
    }

    void RefreshView()
    {
        Items.Clear();

        // Files in current folder
        var files = allFiles
            .Where(f => f.Directory == currentFolder);

        // Folders in current folder
        var folders = allFiles
            .Where(f => f.Directory.StartsWith(currentFolder))
            .Select(f =>
            {
                var remaining = f.Directory.Substring(currentFolder.Length).Trim('/');
                return remaining.Split('/')[0];
            })
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct();

        // Add folders first
        foreach (var folder in folders)
        {
            Items.Add(new ContainerItem
            {
                Name = folder,
                IsFolder = true
            });
        }

        // Then files
        foreach (var file in files)
        {
            Items.Add(new ContainerItem
            {
                Name = file.Name,
                IsFolder = false
            });
        }
    }

    private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (FileList.SelectedItem is ContainerItem item)
        {
            if (item.IsFolder)
            {
                currentFolder = string.IsNullOrEmpty(currentFolder)
                    ? item.Name
                    : $"{currentFolder}/{item.Name}";

                RefreshView();
            }
            else
            {
                var file = allFiles.First(f =>
                    f.Name == item.Name &&
                    f.Directory == currentFolder);

                // 👉 You can preview or extract here
            }
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(currentFolder)) return;

        var parts = currentFolder.Split('/');
        currentFolder = string.Join("/", parts.Take(parts.Length - 1));

        RefreshView();
    }
}
