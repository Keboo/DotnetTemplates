namespace SampleAvaloniaApplication.Views;

public record class DialogRequest(string Title, string Message, IEnumerable<string> Options)
{

}

public record class DialogRequest<TResult> : DialogRequest
{
    private (string Text, TResult Value)[] OptionValues { get; }
    public DialogRequest(string title, string message,
        params (string Text, TResult Value)[] options)
        : base(title, message, options.Select(x => x.Text))
    {
        OptionValues = options;
    }

    public TResult GetValue(string option)
        => OptionValues.FirstOrDefault(x => string.Equals(x.Text, option, StringComparison.Ordinal)).Value;
}
