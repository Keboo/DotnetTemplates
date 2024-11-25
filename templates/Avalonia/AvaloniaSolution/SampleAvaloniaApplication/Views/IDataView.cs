namespace SampleAvaloniaApplication.Views;

public interface IDataView<in T>
{
    Task LoadAsync(T data);
}
