using Avalonia.Controls;

namespace SampleAvaloniaApplication.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainView mainView)
        : this()
    {
        Content = mainView;
    }
}
