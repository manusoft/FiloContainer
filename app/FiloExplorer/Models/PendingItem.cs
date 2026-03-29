using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace FiloExplorer.Models;

public class PendingItem : INotifyPropertyChanged
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Path { get; set; }
    public bool IsDirectory { get; set; }

    public string DisplaySize => IsDirectory ? "Folder" : (new System.IO.FileInfo(Path).Length / 1024).ToString();

    public BitmapImage? Thumbnail { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
