using Avalonia.Controls;

namespace SampleAvaloniaApplication.Views;

public class NavigationRequest
{
    private NavigationRequest(Type viewType)
    {
        ViewType = viewType;
    }

    public virtual Task LoadDataAsync(Control control)
    {
        return Task.CompletedTask;
    }

    public Type ViewType { get; }
    public bool StartNewScope { get; private set; }

    //public static NavigationRequest LoginPage() => new(typeof(LoginView)) { StartNewScope = true };
    // Various nagigation stuff

    private class NavigationRequestData<T> : NavigationRequest
    {
        public NavigationRequestData(Type viewType, T data)
            : base(viewType)
        {
            Data = data;
        }

        public T Data { get; }

        public override Task LoadDataAsync(Control control)
        {
            if (control is IDataView<T> dataView)
            {
                return dataView.LoadAsync(Data);
            }
            return Task.CompletedTask;
        }
    }
}


