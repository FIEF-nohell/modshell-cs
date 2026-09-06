using Avalonia;
using Avalonia.Controls;

namespace modshell_hwtest.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Minimizing hides the window instead of leaving a taskbar entry, since the
    // tray icon is the only affordance needed while polling continues in the
    // background. The hardware loop lives in the view model, not the window,
    // so it keeps running whether the window is shown, hidden or minimized.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty && change.GetNewValue<WindowState>() == WindowState.Minimized)
        {
            Hide();
            ShowInTaskbar = false;
        }
    }

    public void RestoreFromTray()
    {
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Show();
        Activate();
    }
}