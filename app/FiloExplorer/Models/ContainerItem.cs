using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FiloExplorer.Models;

public class ContainerItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsFolder { get; set; }
    public long Size { get; set; }
    public string MimeType { get; set; }
    public bool Encrypted { get; set; }

    public string DisplaySize => IsFolder ? "" : FormatSize(Size);
    public Symbol IconSymbol => IsFolder ? Symbol.Folder : Symbol.Document;
    public Brush IconColor => IsFolder ? new SolidColorBrush(Color.FromArgb(255, 255, 193, 7)) : new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));

    private string FormatSize(long size)
    {
        if (size < 1024) return $"{size} B";
        if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
        if (size < 1024 * 1024 * 1024) return $"{size / 1024.0 / 1024.0:F1} MB";
        return $"{size / 1024.0 / 1024.0 / 1024.0:F1} GB";
    }
}
