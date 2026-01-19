namespace ReactApp.Services;

public class ThemeService
{
    public event Action? OnThemeChanged;

    public bool? IsDarkMode
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnThemeChanged?.Invoke();
            }
        }
    } = null;

    public void ToggleTheme()
    {
        // Once toggled, always toggle between false (light) and true (dark)
        // If currently null (system), determine current value and toggle it
        IsDarkMode = !(IsDarkMode ?? false);
    }
}
