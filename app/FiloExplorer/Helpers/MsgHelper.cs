using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FiloExplorer.Helpers;

public static class MsgHelper
{
    public static async Task ShowMessageDialogAsync(string title, string message, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();
    }

    public static async Task<bool> ShowConfirmationDialogAsync(string title, string message, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public static async Task<string?> ShowPasswordDialogAsync(XamlRoot xamlRoot)
    {
        var passwordBox = new PasswordBox
        {
            PlaceholderText = "Enter container password",
            Width = 320,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "This container is encrypted with AES-256.\nPlease enter the correct password to unlock it.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                passwordBox
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Encrypted Filo Container",
            Content = content,
            PrimaryButtonText = "Unlock",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? passwordBox.Password?.Trim() : null;
    }
}