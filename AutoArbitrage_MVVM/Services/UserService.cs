using System;

namespace AutoArbitrage_MVVM.Services;

public class UserService
{
    private static UserService? _instance;
    public static UserService Instance => _instance ??= new UserService();

    private string? _email;
    public string? Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnEmailChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    

    public event EventHandler? OnEmailChanged;
}
    
    
