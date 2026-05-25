using FiloExplorer.Views;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
using Windows.Graphics;

namespace FiloExplorer;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Modern Fluent Title Bar Setup
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);   // Drag region

        AppWindow.Title = "Filo Explorer";
        AppWindow.Resize(new SizeInt32(1100, 720));

        // Prefer Mica + Dark theme for premium feel
        //AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Dark;

        RootFrame.Navigate(typeof(MainPage));
    }

    private void menuFileNew_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.Navigate(typeof(NewPage));
    }

    private async void menuFileOpen_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(this.RootFrame.XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".filo");

        var file = picker.PickSingleFileAsync().GetAwaiter().GetResult();
        if (file != null)
        {
            RootFrame.Navigate(typeof(ViewPage), file.Path);
        }
    }

    private void menuFileExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void menuHelpView_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.Navigate(typeof(HelpPage));
    }

    private async void menuHelpAbout_Click(object sender, RoutedEventArgs e)
    {
        await ShowAboutDialogAsync();
    }

    private async Task ShowAboutDialogAsync()
    {
        var aboutContent = new StackPanel
        {
            Spacing = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 420
        };

        // App Icon + Name
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        header.Children.Add(new Border
        {
            Width = 80,
            Height = 80,
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue),
            Child = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/AppLogo.png")),
                Stretch = Stretch.UniformToFill
            }
            //Child = new FontIcon
            //{
            //    Glyph = "\uE8A7",        // Container / Archive icon
            //    FontSize = 48,
            //    Foreground = new SolidColorBrush(Colors.White)
            //}
        });

        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };

        titleStack.Children.Add(new TextBlock
        {
            Text = "Filo Explorer",
            FontSize = 28,
            FontWeight = FontWeights.Bold
        });

        titleStack.Children.Add(new TextBlock
        {
            Text = "Version 1.1.0",
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            FontSize = 15
        });

        header.Children.Add(titleStack);
        aboutContent.Children.Add(header);

        // Description
        aboutContent.Children.Add(new TextBlock
        {
            Text = "A modern, secure, and lightweight archive tool for creating and exploring encrypted .filo containers.",
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });

        // Features / Info
        var infoPanel = new StackPanel { Spacing = 12, Margin = new Thickness(0, 20, 0, 0) };

        infoPanel.Children.Add(CreateInfoRow("\uE72E", "AES-256 Encryption"));
        infoPanel.Children.Add(CreateInfoRow("\uE8A5", "Fast Preview for Images & Media"));
        infoPanel.Children.Add(CreateInfoRow("\uE8B7", "Built with WinUI 3 & Fluent Design"));

        aboutContent.Children.Add(infoPanel);

        // Footer / Copyright
        aboutContent.Children.Add(new TextBlock
        {
            Text = "© 2026 ManuHub • All rights reserved",
            FontSize = 13,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 30, 0, 0)
        });

        var dialog = new ContentDialog
        {
            Title = "",                     // We use custom header
            Content = aboutContent,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
        };

        await dialog.ShowAsync();
    }

    // Helper to create nice info rows
    private StackPanel CreateInfoRow(string glyph, string text)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
        {
            new FontIcon
            {
                Glyph = glyph,
                FontSize = 20,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue)
            },
            new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 15
            }
        }
        };
    }
}
