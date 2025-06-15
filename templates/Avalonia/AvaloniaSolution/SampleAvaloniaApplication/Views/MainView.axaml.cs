using Avalonia.Controls;
using Avalonia.Interactivity;

using SampleAvaloniaApplication.ViewModels;

namespace SampleAvaloniaApplication.Views;

public partial class MainView : UserControl
{
    // This constructor is used when the view is created by the XAML Previewer
    public MainView()
    {
        InitializeComponent();
    }

    private Func<MainViewModel>? ViewModelAccessor { get; }

    // This constructor is used when the view is created via dependency injection
    public MainView(Func<MainViewModel> viewModelAccessor)
        : this()
    {
        ViewModelAccessor = viewModelAccessor;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // NB: This intentionally defers creating the view model until after the main view is loaded.
        // This is needed when the view model takes a dependency that requires the application lifetime to initialized.
        // Such as the IStorageProvider.
        DataContext = ViewModelAccessor?.Invoke();
    }
}
