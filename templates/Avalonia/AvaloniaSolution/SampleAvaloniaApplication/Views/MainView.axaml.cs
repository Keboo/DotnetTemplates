using Avalonia.Controls;

using SampleAvaloniaApplication.ViewModels;

namespace SampleAvaloniaApplication.Views;

public partial class MainView : UserControl
{
    // This constructor is used when the view is created by the XAML Previewer
    public MainView()
    {
        InitializeComponent();
    }

    // This constructor is used when the view is created via dependency injection
    public MainView(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
