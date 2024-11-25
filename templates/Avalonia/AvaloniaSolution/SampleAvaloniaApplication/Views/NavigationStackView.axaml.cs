using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;

namespace SampleAvaloniaApplication.Views;

public partial class NavigationStackView : UserControl, IAppShell, IRecipient<NavigationRequest>
{
    private static readonly AttachedProperty<IServiceScope?> ServiceScopeProperty =
        AvaloniaProperty.RegisterAttached<NavigationStackView, StyledElement, IServiceScope?>("ServiceScope");

    private static void SetServiceScope(StyledElement element, IServiceScope? value) =>
        element.SetValue(ServiceScopeProperty, value);
    private static IServiceScope? GetServiceScope(StyledElement element) =>
        element.GetValue(ServiceScopeProperty);

    private IServiceProvider Services { get; }

    private ThicknessTransition DialogTransition { get; }

    public NavigationStackView()
    {
        Services = null!;
        InitializeComponent();
        DialogTransition = DialogHost.Transitions?[0] as ThicknessTransition ?? throw new InvalidOperationException("Transition not found");
    }

    public NavigationStackView(IServiceProvider services, IMessenger messenger)
        : this()
    {
        Services = services;
        messenger.RegisterAll(this);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= MainView_BackRequested;
            topLevel.BackRequested += MainView_BackRequested;
            if (topLevel.InputPane is { } inputPane)
            {
                inputPane.StateChanged -= OnInputPaneStateChanged;
                inputPane.StateChanged += OnInputPaneStateChanged;
            }
        }
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        DialogTransition.Duration = e.AnimationDuration;
        DialogTransition.Easing = e.Easing as Easing ?? new LinearEasing();

        switch (e.NewState)
        {
            case InputPaneState.Closed:
                DialogHost.Margin = new Thickness(0);
                break;
            case InputPaneState.Open:
                DialogHost.Margin = new Thickness(0, 0, 0, e.EndRect.Height);
                break;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= MainView_BackRequested;
            if (topLevel.InputPane is { } inputPane)
            {
                inputPane.StateChanged -= OnInputPaneStateChanged;
            }
        }
        base.OnUnloaded(e);
    }

    private async void MainView_BackRequested(object? sender, RoutedEventArgs e)
    {
        if (await PopAsync())
        {
            e.Handled = true;
        }
    }

    public async Task NavigateAsync(NavigationRequest request)
    {
        Control view = AddView(request, Services);
        while (Root.Children.Count > 1)
        {
            Root.Children.RemoveAt(0);
        }
        await request.LoadDataAsync(view);
    }

    public Task<bool> PopAsync()
    {
        //Don't allow pop to nothing so there must be at least two views
        if (Root.Children.Count > 1)
        {
            //If we have popped a scope root, dispose of the scope
            var topView = Root.Children[^1];
            if (GetServiceScope(topView) is { } serviceScope)
            {
                serviceScope.Dispose();
            }
            Root.Children.RemoveAt(Root.Children.Count - 1);
            if (Root.Children.Count > 0)
            {
                Root.Children[^1].IsEnabled = true;
                Root.Children[^1].IsVisible = true;
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public async Task PushAsync(NavigationRequest request)
    {
        IServiceScope? existingScope = Root.Children
            .Reverse()
            .Select(GetServiceScope)
            .Where(x => x is not null)
            .FirstOrDefault();
        IServiceProvider services = existingScope?.ServiceProvider ?? Services;

        Control view = AddView(request, services);
        for (int i = 0; i < Root.Children.Count - 1; i++)
        {
            Root.Children[i].IsEnabled = false;
            Root.Children[i].IsVisible = false;
        }
        await request.LoadDataAsync(view);
    }

    private Control AddView(NavigationRequest request, IServiceProvider serviceProvider)
    {
        IServiceScope? serviceScope = null;
        if (request.StartNewScope)
        {
            serviceScope = serviceProvider.CreateScope();
            serviceProvider = serviceScope.ServiceProvider;
        }
        Control? view = serviceProvider.GetRequiredService(request.ViewType) as Control
            ?? throw new InvalidOperationException($"View not found for {request.ViewType}");

        SetServiceScope(view, serviceScope);
        Grid.SetRow(view, 0);
        Grid.SetColumn(view, 0);
        view.HorizontalAlignment = HorizontalAlignment.Stretch;
        view.VerticalAlignment = VerticalAlignment.Stretch;
        Root.Children.Add(view);
        return view;
    }

    public Task ShowDialogAsync(DialogRequest request)
    {
        DialogHost.DialogContent = request;
        TaskCompletionSource<object?> tcs = new();
        DialogHost.DialogClosingCallback = (sender, e) =>
        {
            tcs.TrySetResult(e.Parameter);
            DialogHost.DialogClosingCallback = null!;
        };
        DialogHost.IsOpen = true;
        return tcs.Task;
    }

    public Task<TResult> ShowDialogAsync<TResult>(DialogRequest<TResult> request)
    {
        DialogHost.DialogContent = request;
        TaskCompletionSource<TResult> tcs = new();
        DialogHost.DialogClosingCallback = (sender, e) =>
        {
            if (e.Parameter is string stringResult)
            {
                tcs.TrySetResult(request.GetValue(stringResult));
            }
            else
            {
                tcs.TrySetResult(default!);
            }
            DialogHost.DialogClosingCallback = null!;
        };
        DialogHost.IsOpen = true;
        return tcs.Task;
    }

    async void IRecipient<NavigationRequest>.Receive(NavigationRequest request)
    {
        await NavigateAsync(request);
    }
}