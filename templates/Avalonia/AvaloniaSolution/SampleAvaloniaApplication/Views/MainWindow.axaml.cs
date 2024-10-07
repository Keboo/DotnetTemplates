using Avalonia.Controls;

namespace SampleAvaloniaApplication.Views;

public partial class MainWindow : Window
{
    // This constructor is used when the view is created by the XAML Previewer
    public MainWindow()
    {
        InitializeComponent();
    }

    // This constructor is used when the view is created via dependency injection
    public MainWindow(MainView mainView)
        : this()
    {
        Content = mainView;
    }
}
