namespace FiloExplorer.Models;

public class ContainerItem
{
    public string Name { get; set; }
    public bool IsFolder { get; set; }

    public string Type => IsFolder ? "Folder" : "File";
}
