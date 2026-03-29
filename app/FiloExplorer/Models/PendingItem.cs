using Microsoft.UI.Xaml.Media.Imaging;

namespace FiloExplorer.Models;

public class PendingItem
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Path { get; set; }
    public bool IsDirectory { get; set; }

    public BitmapImage? ImageSource { get; set; }
}
