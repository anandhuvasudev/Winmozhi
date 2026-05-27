using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Winmozhi.UI.ViewModels;

namespace Winmozhi.UI.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }
    private readonly IntPtr _hwnd;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_RESTORE = 9;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        ViewModel.RequestShowSettings = WakeUpAndShowSettings;

        try { this.ExtendsContentIntoTitleBar = true; this.SetTitleBar(AppTitleBar); } catch { }

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        try
        {
            string iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "WindowIcon.ico");

            // 1. AppWindow.SetIcon always expects a physical path, so this works for both modes
            if (System.IO.File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }

            // 2. Tray Icon requires different URIs based on how the app is running
            if (IsRunningAsPackaged())
            {
                // Packaged apps (Store / Debug) strictly prefer ms-appx:///
                TrayIcon.IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/WindowIcon.ico"));
            }
            else if (System.IO.File.Exists(iconPath))
            {
                // Unpackaged apps (Inno Setup) require the absolute local file path
                TrayIcon.IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(iconPath));
            }
        }
        catch { }

        AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            ShowWindow(_hwnd, SW_HIDE);
            Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
        };
    }

    private static bool IsRunningAsPackaged()
    {
        try
        {
            return Windows.ApplicationModel.Package.Current != null;
        }
        catch
        {
            return false;
        }
    }

    public void ShowSettings() => WakeUpAndShowSettings();

    private void WakeUpAndShowSettings()
    {
        this.DispatcherQueue.TryEnqueue(async () =>
        {
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);

            int width = 480;
            int height = 760;
            int x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - width) / 2;
            int y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - height) / 2;

            AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

            ShowWindow(_hwnd, SW_RESTORE);
            SetForegroundWindow(_hwnd);
            this.Activate();

            // First-Run Logic: Automatically show tutorial if first time opening
            if (Winmozhi.Core.Utilities.LocalPreferences.IsFirstRun)
            {
                Winmozhi.Core.Utilities.LocalPreferences.IsFirstRun = false;
                Winmozhi.Core.Utilities.LocalPreferences.Save();

                await Task.Delay(200);
                await ShowHowToUseDialogAsync();
            }
        });
    }

    private void ShowHowToUse_Click(object sender, RoutedEventArgs e)
    {
        _ = ShowHowToUseDialogAsync();
    }

    private async Task ShowHowToUseDialogAsync()
    {
        var mainPanel = new StackPanel { Spacing = 20, Margin = new Thickness(0, 10, 0, 0) };

        // 1. Getting Started Header
        var header1 = new TextBlock
        {
            Text = "Getting Started",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        mainPanel.Children.Add(header1);

        // 2. Beautiful Numbered Steps with Custom Badges
        var stepsPanel = new StackPanel { Spacing = 14, Margin = new Thickness(4, 0, 0, 0) };
        stepsPanel.Children.Add(CreateStepItem("1", "Type anywhere in Manglish (e.g., 'njan')."));
        stepsPanel.Children.Add(CreateStepItem("2", "A popup will instantly appear with Malayalam suggestions."));
        stepsPanel.Children.Add(CreateStepItem("3", "Press Space or Enter to insert the highlighted word."));
        stepsPanel.Children.Add(CreateStepItem("4", "Use the Up and Down arrow keys to choose alternative suggestions."));
        mainPanel.Children.Add(stepsPanel);

        // 3. Elegant Divider Line
        var divider = new Border
        {
            Height = 1,
            Background = (SolidColorBrush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            Margin = new Thickness(0, 8, 0, 8)
        };
        mainPanel.Children.Add(divider);

        // 4. Global Shortcut Header
        var header2 = new TextBlock
        {
            Text = "Global Shortcut",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        mainPanel.Children.Add(header2);

        // 5. Formatted Text with bold highlighted keys
        var shortcutDesc = new RichTextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 0, 0) };
        var para = new Microsoft.UI.Xaml.Documents.Paragraph();

        para.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = "Press ", Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

        para.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run
        {
            Text = "Ctrl + Shift + M",
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        });

        para.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run
        {
            Text = " at any time to temporarily pause or resume the transliteration engine across your entire system.",
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        shortcutDesc.Blocks.Add(para);
        mainPanel.Children.Add(shortcutDesc);

        // 6. Native Opaque Dialog (No Mica Transparency)
        var dialog = new ContentDialog
        {
            Title = "How to use Winmozhi",
            Content = mainPanel,
            CloseButtonText = "Got it",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    // Fixed CA1822: Marked helper method as static since it doesn't use instance members
    private static Grid CreateStepItem(string number, string description)
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Creates a circular badge matching your Windows Accent Color
        var numBadge = new Border
        {
            Background = (SolidColorBrush)Application.Current.Resources["SystemControlBackgroundAccentBrush"],
            CornerRadius = new CornerRadius(12),
            Width = 24,
            Height = 24,
            Child = new TextBlock
            {
                Text = number,
                Foreground = (SolidColorBrush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            }
        };
        Grid.SetColumn(numBadge, 0);

        var descBlock = new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };
        Grid.SetColumn(descBlock, 1);

        grid.Children.Add(numBadge);
        grid.Children.Add(descBlock);

        return grid;
    }
}