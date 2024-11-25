namespace SampleAvaloniaApplication.Views;

public interface IAppShell
{
    Task NavigateAsync(NavigationRequest request);
    Task PushAsync(NavigationRequest request);
    Task<bool> PopAsync();

    Task ShowDialogAsync(DialogRequest request);
    Task<TResult> ShowDialogAsync<TResult>(DialogRequest<TResult> request);
}
