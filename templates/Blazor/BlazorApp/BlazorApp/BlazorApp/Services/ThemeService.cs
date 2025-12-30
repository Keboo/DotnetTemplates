namespace BlazorApp.Services;

public class ThemeService
{
    public event Action? OnThemeChanged;
    
    private bool? _isDarkMode = null; // null = system preference (initial only)
    
    public bool? IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
            }
        }
    }
    
    public void ToggleTheme()
    {
        // Once toggled, always toggle between false (light) and true (dark)
        // If currently null (system), determine current value and toggle it
        IsDarkMode = !(_isDarkMode ?? false);
    }
}
