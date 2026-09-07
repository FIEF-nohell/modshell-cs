using System;
using Avalonia;
using Avalonia.Controls;

namespace modshell_cs.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
    }

    // The title-bar close button sends the monitor to tray. The only real
    // shutdown path is the tray menu's Close item.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            WindowState = WindowState.Normal;
            ShowInTaskbar = false;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    public void CloseFromTray()
    {
        _allowClose = true;
        Close();
    }

    public void RestoreFromTray()
    {
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Show();
        Activate();
    }
}
