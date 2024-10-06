using Avalonia.Controls;

using SampleAvaloniaApplication.ViewModels;

namespace SampleAvaloniaApplication.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public MainView(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
