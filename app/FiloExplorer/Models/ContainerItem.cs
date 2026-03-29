using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

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
    public Symbol FontIcon => IsFolder ? Symbol.Folder : Symbol.Document;

    private string FormatSize(long size)
    {
        if (size < 1024) return $"{size} B";
        if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
        if (size < 1024 * 1024 * 1024) return $"{size / 1024.0 / 1024.0:F1} MB";
        return $"{size / 1024.0 / 1024.0 / 1024.0:F1} GB";
    }
}
