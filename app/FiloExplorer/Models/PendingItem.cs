using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace FiloExplorer.Models;

public class PendingItem : INotifyPropertyChanged
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Path { get; set; }
    public bool IsDirectory { get; set; }

    public string DisplaySize => IsDirectory ? "" : FormatSize(new System.IO.FileInfo(Path).Length);

    public BitmapImage? Thumbnail { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string FormatSize(long size)
    {
        if (size < 1024) return $"{size} B";
        if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
        if (size < 1024 * 1024 * 1024) return $"{size / 1024.0 / 1024.0:F1} MB";
        return $"{size / 1024.0 / 1024.0 / 1024.0:F1} GB";
    }
}
